using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Client.Security;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.Threading;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Main desktop client window for Avalonia.RemoteControl.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<RemoteTreeItem> treeItems = [];
    private readonly ObservableCollection<PropertyRow> propertyRows = [];
    private readonly ObservableCollection<string> logRows = [];
    private readonly IRemoteControlProfileStore profileStore = new FileRemoteControlProfileStore();
    private RemoteControlDesktopSession? session;
    private TreeNode? selectedNode;
    private CancellationTokenSource? logStreamCancellation;
    private RemoteControlServerCertificateInfo? pendingCertificateInfo;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        ControlTree.ItemsSource = treeItems;
        PropertyList.ItemsSource = propertyRows;
        LogList.ItemsSource = logRows;

        Closing += (_, _) =>
        {
            logStreamCancellation?.Cancel();
            session?.Dispose();
        };

        _ = LoadProfileAsync();
    }

    private async void ConnectClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            logStreamCancellation?.Cancel();
            session?.Dispose();
            session = RemoteControlDesktopSession.Create(
                new Uri(EndpointBox.Text ?? string.Empty),
                TokenBox.Text ?? string.Empty,
                CertificatePathBox.Text,
                acceptedServerCertificateSha256Fingerprint: AcceptedFingerprintBox.Text);

            var capabilities = await session.GetCapabilitiesAsync();
            StatusText.Text = $"Connected: protocol {capabilities.ProtocolVersion}";
            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Connection failed: {ex.Message}";
        }
    }

    private async void SaveProfileClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            await SaveCurrentProfileAsync("Connection profile saved.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Save failed: {ex.Message}";
        }
    }

    private async void ForgetProfileClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            await profileStore.ForgetDefaultAsync();
            TokenBox.Text = string.Empty;
            CertificatePathBox.Text = string.Empty;
            AcceptedFingerprintBox.Text = string.Empty;
            pendingCertificateInfo = null;
            StatusText.Text = "Saved connection profile forgotten.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Forget failed: {ex.Message}";
        }
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            var profile = await profileStore.LoadDefaultAsync();

            if (profile is null)
            {
                return;
            }

            EndpointBox.Text = profile.Endpoint;
            TokenBox.Text = profile.Token;
            CertificatePathBox.Text = profile.CertificatePath;
            AcceptedFingerprintBox.Text = profile.AcceptedServerCertificateSha256Fingerprint;
            StatusText.Text = "Saved connection profile loaded.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Profile load failed: {ex.Message}";
        }
    }

    private async void RefreshSnapshotClicked(object? sender, RoutedEventArgs e)
    {
        await RefreshSnapshotAsync();
    }

    private async Task RefreshSnapshotAsync()
    {
        if (session is null)
        {
            StatusText.Text = "Connect before requesting a snapshot.";
            return;
        }

        try
        {
            var snapshot = await session.GetSnapshotAsync();
            selectedNode = null;
            propertyRows.Clear();
            treeItems.Clear();

            foreach (var item in BuildTree(snapshot))
            {
                treeItems.Add(item);
            }

            StatusText.Text = $"Snapshot {snapshot.Sequence}: {snapshot.Nodes.Count} nodes";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Snapshot failed: {ex.Message}";
        }
    }

    private void ControlTreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.AddedItems.OfType<RemoteTreeItem>().FirstOrDefault();
        selectedNode = item?.Node;
        propertyRows.Clear();

        if (selectedNode is null)
        {
            return;
        }

        foreach (var property in selectedNode.Properties)
        {
            propertyRows.Add(new PropertyRow(
                property.Name,
                property.Value,
                $"{property.Name} = {property.Value} ({property.ValueType})"));
        }

        StatusText.Text = $"Selected {selectedNode.TypeName} {selectedNode.Name}".TrimEnd();
    }

    private void PropertySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.OfType<PropertyRow>().FirstOrDefault() is { } row)
        {
            PropertyNameBox.Text = row.Name;
            PropertyValueBox.Text = row.Value;
        }
    }

    private async void InvokeClickClicked(object? sender, RoutedEventArgs e)
    {
        if (session is null || selectedNode is null)
        {
            StatusText.Text = "Select a node before invoking a click.";
            return;
        }

        try
        {
            var result = await session.InvokeClickAsync(selectedNode.Id);
            StatusText.Text = result.Message;
            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Click failed: {ex.Message}";
        }
    }

    private async void InvokeFocusClicked(object? sender, RoutedEventArgs e)
    {
        if (session is null || selectedNode is null)
        {
            StatusText.Text = "Select a node before requesting focus.";
            return;
        }

        try
        {
            var result = await session.InvokeFocusAsync(selectedNode.Id);
            StatusText.Text = result.Message;
            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Focus failed: {ex.Message}";
        }
    }

    private async void SetPropertyClicked(object? sender, RoutedEventArgs e)
    {
        if (session is null || selectedNode is null)
        {
            StatusText.Text = "Select a node before setting a property.";
            return;
        }

        try
        {
            var result = await session.SetPropertyAsync(
                selectedNode.Id,
                PropertyNameBox.Text ?? string.Empty,
                PropertyValueBox.Text ?? string.Empty);

            StatusText.Text = result.Message;
            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Property update failed: {ex.Message}";
        }
    }

    private void ToggleLogsClicked(object? sender, RoutedEventArgs e)
    {
        if (session is null)
        {
            StatusText.Text = "Connect before streaming logs.";
            return;
        }

        if (logStreamCancellation is not null)
        {
            logStreamCancellation.Cancel();
            logStreamCancellation = null;
            StatusText.Text = "Log stream stopped.";
            return;
        }

        logStreamCancellation = new CancellationTokenSource();
        _ = WatchLogsAsync(logStreamCancellation.Token);
        StatusText.Text = "Log stream started.";
    }

    private async Task WatchLogsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var entry in session!.WatchLogsAsync("Information", null, cancellationToken))
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    logRows.Add($"{entry.Sequence} {entry.Level} {entry.Category}: {entry.Message}");
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
                StatusText.Text = $"Log stream failed: {ex.Message}";
            });
        }
    }

    private async void InspectCertificateClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            pendingCertificateInfo =
                await RemoteControlServerCertificateInspector.InspectAsync(
                    new Uri(EndpointBox.Text ?? string.Empty));

            StatusText.Text =
                $"Certificate {pendingCertificateInfo.Subject}; SHA-256 {pendingCertificateInfo.Sha256Fingerprint}";
        }
        catch (Exception ex)
        {
            pendingCertificateInfo = null;
            StatusText.Text = $"Certificate inspection failed: {ex.Message}";
        }
    }

    private async void AcceptCertificateClicked(object? sender, RoutedEventArgs e)
    {
        if (pendingCertificateInfo is null)
        {
            StatusText.Text = "Inspect a TLS certificate before accepting it.";
            return;
        }

        try
        {
            AcceptedFingerprintBox.Text = pendingCertificateInfo.Sha256Fingerprint;
            await SaveCurrentProfileAsync("Certificate accepted and profile saved.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Certificate accept failed: {ex.Message}";
        }
    }

    private async void RejectCertificateClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            pendingCertificateInfo = null;
            AcceptedFingerprintBox.Text = string.Empty;
            await SaveCurrentProfileAsync("Certificate trust cleared.");
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Certificate reject failed: {ex.Message}";
        }
    }

    private async Task SaveCurrentProfileAsync(string statusText)
    {
        await profileStore.SaveDefaultAsync(new RemoteControlConnectionProfile
        {
            Endpoint = EndpointBox.Text ?? string.Empty,
            Token = TokenBox.Text ?? string.Empty,
            CertificatePath = CertificatePathBox.Text ?? string.Empty,
            AcceptedServerCertificateSha256Fingerprint = AcceptedFingerprintBox.Text ?? string.Empty,
            UpdatedUtc = DateTimeOffset.UtcNow,
        });

        StatusText.Text = statusText;
    }

    private static IReadOnlyList<RemoteTreeItem> BuildTree(TreeSnapshot snapshot)
    {
        var nodesById = snapshot.Nodes.ToDictionary(
            node => node.Id,
            node => new RemoteTreeItem(node));
        var roots = new List<RemoteTreeItem>();

        foreach (var node in snapshot.Nodes)
        {
            var item = nodesById[node.Id];

            if (string.IsNullOrWhiteSpace(node.ParentId) || !nodesById.TryGetValue(node.ParentId, out var parent))
            {
                roots.Add(item);
            }
            else
            {
                parent.Children.Add(item);
            }
        }

        return roots;
    }
}

/// <summary>
/// View model for a remote control tree node.
/// </summary>
public sealed class RemoteTreeItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteTreeItem"/> class.
    /// </summary>
    /// <param name="node">The protocol tree node.</param>
    public RemoteTreeItem(TreeNode node)
    {
        Node = node;
        Header = string.IsNullOrWhiteSpace(node.Name)
            ? node.TypeName
            : $"{node.TypeName} {node.Name}";
    }

    /// <summary>
    /// Gets the protocol tree node.
    /// </summary>
    public TreeNode Node { get; }

    /// <summary>
    /// Gets the display header.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets child tree items.
    /// </summary>
    public ObservableCollection<RemoteTreeItem> Children { get; } = [];
}

/// <summary>
/// View model for a selected node property.
/// </summary>
/// <param name="Name">Property name.</param>
/// <param name="Value">Current value.</param>
/// <param name="Display">Display text.</param>
public sealed record PropertyRow(string Name, string Value, string Display)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return Display;
    }
}
