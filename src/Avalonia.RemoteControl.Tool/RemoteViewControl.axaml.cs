using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Client.Live;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.Threading;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Reusable live remote UI surface for windowed and docked hosts.
/// </summary>
public sealed partial class RemoteViewControl : UserControl
{
    private readonly RemoteControlDesktopSession? session;
    private readonly RemoteLiveViewCapabilities capabilities;
    private readonly RemoteLiveTreeModel treeModel = new();
    private CancellationTokenSource? streamCancellation;
    private CancellationTokenSource? frameCancellation;
    private RemoteViewCoordinateMapper mapper = RemoteViewCoordinateMapper.Create(1, 1, 1, 1);
    private double remoteWidth = 1;
    private double remoteHeight = 1;
    private bool showScreenshot;
    private bool started;
    private bool frameStreamStarted;
    private bool moveSendScheduled;
    private bool inputUnsupportedStatusShown;
    private RemoteInputEvent? pendingMove;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteViewControl"/> class for XAML tooling.
    /// </summary>
    public RemoteViewControl()
    {
        InitializeComponent();
        capabilities = RemoteLiveViewCapabilities.None;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteViewControl"/> class.
    /// </summary>
    /// <param name="session">Remote-control session.</param>
    /// <param name="capabilities">Endpoint live-view capabilities.</param>
    public RemoteViewControl(
        RemoteControlDesktopSession session,
        RemoteLiveViewCapabilities capabilities)
    {
        this.session = session;
        this.capabilities = capabilities;
        InitializeComponent();
        ApplyCapabilityState();

        AttachedToVisualTree += (_, _) => Start();
        DetachedFromVisualTree += (_, _) => Stop();
        ViewportBorder.SizeChanged += (_, _) => UpdateMapper();
    }

    /// <summary>
    /// Raised when a live-view click resolves to a remote tree node.
    /// </summary>
    public event EventHandler<string>? RemoteNodeClicked;

    /// <summary>
    /// Raised after live input is sent to the remote app.
    /// </summary>
    public event EventHandler<RemoteInputRecordedEventArgs>? RemoteInputSent;

    /// <summary>
    /// Starts live-view streams if the control is not already running.
    /// </summary>
    public void Start()
    {
        if (started || session is null)
        {
            return;
        }

        started = true;
        streamCancellation = new CancellationTokenSource();
        ViewportBorder.Focus();
        _ = WatchTreeOrPollAsync(streamCancellation.Token);
        if (showScreenshot)
        {
            StartFrameStream();
        }
    }

    /// <summary>
    /// Stops live-view streams.
    /// </summary>
    public void Stop()
    {
        started = false;
        StopFrameStream();
        streamCancellation?.Cancel();
        streamCancellation?.Dispose();
        streamCancellation = null;
    }

    private void ScreenshotModeClicked(object? sender, RoutedEventArgs e)
    {
        if (!capabilities.SupportsFrameStreaming)
        {
            showScreenshot = false;
            FrameImage.IsVisible = false;
            StatusText.Text = "Frame streaming is not supported by this endpoint.";
            RenderOverlay();
            return;
        }

        showScreenshot = true;
        FrameImage.IsVisible = true;
        StatusText.Text = "Starting frame stream...";
        StartFrameStream();
        RenderOverlay();
    }

    private void TreeModeClicked(object? sender, RoutedEventArgs e)
    {
        showScreenshot = false;
        FrameImage.IsVisible = false;
        StopFrameStream();
        StatusText.Text = "Frame streaming disabled; using tree replica mode.";
        RenderOverlay();
    }

    private void OverlayChanged(object? sender, RoutedEventArgs e)
    {
        RenderOverlay();
    }

    private void ApplyCapabilityState()
    {
        ScreenshotModeButton.IsEnabled = capabilities.SupportsFrameStreaming;
        showScreenshot = false;
        FrameImage.IsVisible = false;

        if (!capabilities.SupportsFrameStreaming)
        {
            StatusText.Text = capabilities.SupportsTreeStreaming || capabilities.SupportsTreeSnapshots
                ? "Frame streaming is not supported; using tree replica mode."
                : "Live view is not supported by this endpoint.";
        }
        else
        {
            StatusText.Text = "Frame streaming disabled; using tree replica mode.";
        }

        if (!capabilities.SupportsRemoteInput)
        {
            ToolTip.SetTip(ViewportBorder, "Remote input is not supported or not enabled for this endpoint.");
        }
    }

    private void StartFrameStream()
    {
        if (!started || session is null || frameStreamStarted || !capabilities.SupportsFrameStreaming)
        {
            return;
        }

        frameStreamStarted = true;
        frameCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            streamCancellation?.Token ?? CancellationToken.None);
        _ = WatchFramesAsync(frameCancellation.Token);
    }

    private void StopFrameStream()
    {
        frameStreamStarted = false;
        frameCancellation?.Cancel();
        frameCancellation?.Dispose();
        frameCancellation = null;
    }

    private Task WatchTreeOrPollAsync(CancellationToken cancellationToken)
    {
        if (capabilities.SupportsTreeStreaming)
        {
            return WatchTreeAsync(cancellationToken);
        }

        return capabilities.SupportsTreeSnapshots
            ? PollSnapshotsAsync(cancellationToken)
            : ReportUnsupportedTreeAsync();
    }

    private async Task ReportUnsupportedTreeAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            StatusText.Text = "Tree snapshots are not supported by this endpoint.";
        });
    }

    private async Task WatchTreeAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var update in session!.WatchTreeAsync(cancellationToken))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    treeModel.ApplySnapshot(update.Snapshot);
                    UpdateRemoteSizeFromTree();
                    RenderOverlay();
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = $"Tree stream failed: {ex.Message}");
        }
    }

    private async Task PollSnapshotsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var snapshot = await session!.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    treeModel.ApplySnapshot(snapshot);
                    UpdateRemoteSizeFromTree();
                    RenderOverlay();
                });

                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = $"Tree snapshot polling failed: {ex.Message}");
        }
    }

    private async Task WatchFramesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var frame in session!.WatchFramesAsync(cancellationToken))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using var stream = new MemoryStream(frame.Png.ToByteArray());
                    FrameImage.Source = new Bitmap(stream);
                    remoteWidth = Math.Max(1, frame.RootWidth);
                    remoteHeight = Math.Max(1, frame.RootHeight);
                    UpdateMapper();
                    StatusText.Text = $"Frame {frame.Sequence}: {frame.PixelWidth}x{frame.PixelHeight}";
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusText.Text = $"Frame stream unavailable: {ex.Message}";
                showScreenshot = false;
                FrameImage.IsVisible = false;
                RenderOverlay();
            });
        }
    }

    private void ViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewportBorder.Focus();
        var remote = ToRemote(e.GetPosition(ViewportBorder));
        SelectNodeAt(remote);
        _ = SendInputAsync(new RemoteInputEvent
        {
            Kind = RemoteInputKind.PointerPress,
            Button = RemoteMouseButton.Left,
            X = remote.X,
            Y = remote.Y,
        });
        e.Handled = true;
    }

    private void ViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var remote = ToRemote(e.GetPosition(ViewportBorder));
        _ = SendInputAsync(new RemoteInputEvent
        {
            Kind = RemoteInputKind.PointerRelease,
            Button = RemoteMouseButton.Left,
            X = remote.X,
            Y = remote.Y,
        });
        e.Handled = true;
    }

    private void ViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        var remote = ToRemote(e.GetPosition(ViewportBorder));
        pendingMove = new RemoteInputEvent
        {
            Kind = RemoteInputKind.PointerMove,
            X = remote.X,
            Y = remote.Y,
        };

        if (!moveSendScheduled)
        {
            moveSendScheduled = true;
            _ = SendPendingMoveAsync();
        }
    }

    private void ViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var remote = ToRemote(e.GetPosition(ViewportBorder));
        _ = SendInputAsync(new RemoteInputEvent
        {
            Kind = RemoteInputKind.Wheel,
            X = remote.X,
            Y = remote.Y,
            DeltaX = e.Delta.X,
            DeltaY = e.Delta.Y,
        });
        e.Handled = true;
    }

    private void ViewportKeyDown(object? sender, KeyEventArgs e)
    {
        _ = SendInputAsync(new RemoteInputEvent
        {
            Kind = RemoteInputKind.KeyDown,
            Key = e.Key.ToString(),
        });
    }

    private void ViewportKeyUp(object? sender, KeyEventArgs e)
    {
        _ = SendInputAsync(new RemoteInputEvent
        {
            Kind = RemoteInputKind.KeyUp,
            Key = e.Key.ToString(),
        });
    }

    private void ViewportTextInput(object? sender, TextInputEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Text))
        {
            _ = SendInputAsync(new RemoteInputEvent
            {
                Kind = RemoteInputKind.Text,
                Text = e.Text,
            });
        }
    }

    private async Task SendPendingMoveAsync()
    {
        try
        {
            await Task.Delay(16, streamCancellation?.Token ?? CancellationToken.None).ConfigureAwait(false);
            var move = pendingMove;
            pendingMove = null;

            if (move is not null)
            {
                await SendInputAsync(move).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            moveSendScheduled = false;
        }
    }

    private async Task SendInputAsync(RemoteInputEvent inputEvent)
    {
        if (!capabilities.SupportsRemoteInput)
        {
            if (!inputUnsupportedStatusShown)
            {
                inputUnsupportedStatusShown = true;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusText.Text = "Remote input is not supported or not enabled for this endpoint.";
                });
            }

            return;
        }

        try
        {
            await session!.SendInputAsync(
                [inputEvent],
                streamCancellation?.Token ?? CancellationToken.None).ConfigureAwait(false);
            RemoteInputSent?.Invoke(this, new RemoteInputRecordedEventArgs([inputEvent]));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() => StatusText.Text = $"Input failed: {ex.Message}");
        }
    }

    private RemoteViewPoint ToRemote(Point point)
    {
        return mapper.ToRemote(point.X, point.Y);
    }

    private void SelectNodeAt(RemoteViewPoint remote)
    {
        var node = treeModel.HitTest(remote.X, remote.Y);
        treeModel.SelectNode(node?.Id);

        if (node is not null)
        {
            RemoteNodeClicked?.Invoke(this, node.Id);
            StatusText.Text = string.IsNullOrWhiteSpace(node.Name)
                ? $"Selected {node.TypeName}"
                : $"Selected {node.TypeName} {node.Name}";
        }

        RenderOverlay();
    }

    private void UpdateRemoteSizeFromTree()
    {
        if (treeModel.Nodes.Count == 0)
        {
            return;
        }

        var width = treeModel.Nodes.Max(static node => node.AbsoluteBounds.X + node.AbsoluteBounds.Width);
        var height = treeModel.Nodes.Max(static node => node.AbsoluteBounds.Y + node.AbsoluteBounds.Height);

        remoteWidth = Math.Max(1, width);
        remoteHeight = Math.Max(1, height);
        UpdateMapper();
    }

    private void UpdateMapper()
    {
        mapper = RemoteViewCoordinateMapper.Create(
            remoteWidth,
            remoteHeight,
            Math.Max(1, ViewportBorder.Bounds.Width),
            Math.Max(1, ViewportBorder.Bounds.Height));
        RenderOverlay();
    }

    private void RenderOverlay()
    {
        OverlayCanvas.Children.Clear();

        var shouldShowOverlay = showScreenshot
            ? OverlayCheckBox.IsChecked == true
            : true;

        if (!shouldShowOverlay)
        {
            return;
        }

        foreach (var node in treeModel.Nodes.Where(static node => node.IsVisible))
        {
            var topLeft = mapper.ToViewport(node.AbsoluteBounds.X, node.AbsoluteBounds.Y);
            var bottomRight = mapper.ToViewport(
                node.AbsoluteBounds.X + node.AbsoluteBounds.Width,
                node.AbsoluteBounds.Y + node.AbsoluteBounds.Height);
            var width = Math.Max(1, bottomRight.X - topLeft.X);
            var height = Math.Max(1, bottomRight.Y - topLeft.Y);

            var rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = node.Id == treeModel.SelectedNodeId
                    ? Brushes.Gold
                    : node.IsFocused
                        ? Brushes.DeepSkyBlue
                        : Brushes.LimeGreen,
                StrokeThickness = node.Id == treeModel.SelectedNodeId || node.IsFocused ? 2 : 1,
                Fill = showScreenshot ? Brushes.Transparent : new SolidColorBrush(Color.FromArgb(32, 0, 200, 120)),
            };
            Canvas.SetLeft(rectangle, topLeft.X);
            Canvas.SetTop(rectangle, topLeft.Y);
            OverlayCanvas.Children.Add(rectangle);

            if (!showScreenshot && height >= 12 && width >= 24)
            {
                var label = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(node.Name)
                        ? node.TypeName
                        : $"{node.TypeName} {node.Name}",
                    Foreground = Brushes.White,
                    FontSize = 11,
                };
                Canvas.SetLeft(label, topLeft.X + 2);
                Canvas.SetTop(label, topLeft.Y + 1);
                OverlayCanvas.Children.Add(label);
            }
        }
    }
}

/// <summary>
/// Event arguments for live input that was sent to the remote app.
/// </summary>
/// <param name="Events">Sent input events.</param>
public sealed record RemoteInputRecordedEventArgs(IReadOnlyList<RemoteInputEvent> Events);
