using Avalonia;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Diagnostics;
using Avalonia.RemoteControl.Client.Live;
using Avalonia.RemoteControl.Client.Logging;
using Avalonia.RemoteControl.Client.Projects;
using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Client.Security;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Tool.Docking;
using Avalonia.Threading;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Main desktop client window for Avalonia.RemoteControl.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const int MaxDisplayedLogRows = 2000;
    private readonly RemoteControlToolShellViewModel shellView = new();
    private readonly ObservableCollection<AdbDeviceItem> adbDevices = [];
    private readonly IRemoteControlProfileStore profileStore = new FileRemoteControlProfileStore();
    private readonly FileRemoteControlProjectStore projectStore = new();
    private readonly SemaphoreSlim projectSaveLock = new(1, 1);
    private readonly DispatcherTimer layoutSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private RemoteControlProjectDocument projectDocument =
        RemoteControlProjectDocument.Create(RemoteControlProjectIds.DefaultProjectId, RemoteControlProjectIds.DefaultProjectName);
    private RemoteControlProjectSessionRecorder? projectRecorder;
    private RemoteControlDesktopSession? session;
    private RemoteControlMcpHostController? mcpHostController;
    private TreeNode? selectedNode;
    private TreeSnapshot? lastSnapshot;
    private CancellationTokenSource? logStreamCancellation;
    private RemoteControlServerCertificateInfo? pendingCertificateInfo;
    private readonly RemoteControlDockFactory dockFactory;
    private readonly RemoteControlDockLayoutStore dockLayoutStore = new();
    private RemoteViewControl? dockedLiveViewControl;
    private bool isClosing;
    private bool isApplyingLayoutState;
    private bool isProjectLoaded;

    private ControlTreePanelViewModel controlTreeView => shellView.ControlTree;

    private WorkspacePanelViewModel workspaceView => shellView.Workspace;

    private RemoteToolsPanelViewModel remoteToolsView => shellView.RemoteTools;

    private RemoteLogViewModel logView => shellView.Logs;

    private RemoteLiveViewCapabilities liveViewCapabilities
    {
        get => shellView.LiveViewCapabilities;
        set => shellView.LiveViewCapabilities = value;
    }

    private bool restoreDockedLiveViewOnConnect
    {
        get => shellView.RestoreDockedLiveViewOnConnect;
        set => shellView.RestoreDockedLiveViewOnConnect = value;
    }

    private ObservableCollection<RemoteTreeItem> treeItems => controlTreeView.Items;

    private PropertiesPanelViewModel propertiesView => workspaceView.Properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        dockFactory = new RemoteControlDockFactory(shellView);
        var layout = dockFactory.CreateLayout();
        dockFactory.InitLayout(layout);
        shellView.Layout = layout;
        shellView.DockFactory = dockFactory;
        Dock.Factory = dockFactory;
        Dock.Layout = layout;

        controlTreeView.PropertyChanged += ControlTreeViewPropertyChanged;
        workspaceView.PropertyChanged += WorkspaceViewPropertyChanged;
        propertiesView.PropertyChanged += PropertiesViewPropertyChanged;
        propertiesView.PropertyEditRequested += (_, args) => PropertyGridEditRequested(args);
        workspaceView.Terminal.PropertyChanged += WorkspaceTerminalPropertyChanged;
        remoteToolsView.PropertyChanged += RemoteToolsViewPropertyChanged;
        remoteToolsView.Actions.InvokeClickRequested += (_, _) => InvokeClickClicked(null, new RoutedEventArgs());
        remoteToolsView.Actions.FocusRequested += (_, _) => InvokeFocusClicked(null, new RoutedEventArgs());
        remoteToolsView.Actions.SetPropertyRequested += (_, _) => SetPropertyClicked(null, new RoutedEventArgs());
        remoteToolsView.Project.SaveProjectRequested += (_, _) => SaveProjectClicked(null, new RoutedEventArgs());
        remoteToolsView.Project.RefreshRequested += (_, _) => RefreshProjectClicked(null, new RoutedEventArgs());
        logView.PropertyChanged += LogViewPropertyChanged;
        AdbDeviceBox.ItemsSource = adbDevices;
        TransportProtocolBox.ItemsSource = new[]
        {
            RemoteControlProtocol.GrpcTransportProtocol,
            RemoteControlProtocol.AndroidBridgeTransportProtocol,
        };
        TransportProtocolBox.SelectedItem = RemoteControlProtocol.GrpcTransportProtocol;
        mcpHostController = new RemoteControlMcpHostController(
            workspaceView.Terminal,
            CreateMcpOptionsFromTerminalState);
        mcpHostController.Start();
        EndpointBox.TextChanged += (_, _) => UpdateTerminalMcpProfileFromFields();
        TokenBox.TextChanged += (_, _) => UpdateTerminalMcpProfileFromFields();
        CertificatePathBox.TextChanged += (_, _) => UpdateTerminalMcpProfileFromFields();
        AcceptedFingerprintBox.TextChanged += (_, _) => UpdateTerminalMcpProfileFromFields();
        TransportProtocolBox.SelectionChanged += (_, _) => UpdateTerminalMcpProfileFromFields();
        AdbPathBox.Text = ProcessAdbCommandRunner.ResolveDefaultAdbPath();
        SizeChanged += (_, _) => ScheduleLayoutSave();
        layoutSaveTimer.Tick += (_, _) =>
        {
            layoutSaveTimer.Stop();
            _ = SaveProjectAsync();
        };
        UpdateLogStreamStatus("Log stream stopped.");
        UpdateTerminalMcpProfileFromFields();
        UpdateProjectStatus();

        Closing += (_, _) =>
        {
            isClosing = true;
            layoutSaveTimer.Stop();
            CaptureLayoutState();
            StopLogStream(addRow: false);
            StopDockedLiveView();
            projectRecorder?.Complete();
            mcpHostController?.Dispose();
            session?.Dispose();
            _ = SaveProjectAsync(captureLayout: false);
        };

        _ = LoadProjectAsync();
        _ = LoadProfileAsync();
    }

    private void ControlTreeViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(ControlTreePanelViewModel.SelectedItem), StringComparison.Ordinal))
        {
            ControlTreeSelectionChanged(controlTreeView.SelectedItem);
        }
    }

    private void PropertiesViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(PropertiesPanelViewModel.SelectedItem), StringComparison.Ordinal))
        {
            PropertySelectionChanged(propertiesView.SelectedItem);
        }
    }

    private void WorkspaceViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(WorkspacePanelViewModel.SelectedTabIndex), StringComparison.Ordinal))
        {
            ScheduleLayoutSave();
        }
    }

    private void RemoteToolsViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(RemoteToolsPanelViewModel.SelectedTabIndex), StringComparison.Ordinal))
        {
            ScheduleLayoutSave();
        }
    }

    private async void ConnectClicked(object? sender, RoutedEventArgs e)
    {
        await ConnectFromFieldsAsync();
    }

    private async Task ConnectFromFieldsAsync(bool prepareAdbForward = true)
    {
        try
        {
            StopLogStream(addRow: false);
            StopDockedLiveView();
            session?.Dispose();
            logView.Rows.Clear();
            if (prepareAdbForward && ShouldPrepareAdbForwardForConnect())
            {
                await PrepareAdbForwardFromFieldsAsync(requirePackageName: false);
            }

            session = RemoteControlDesktopSession.Create(
                new Uri(EndpointBox.Text ?? string.Empty),
                TokenBox.Text ?? string.Empty,
                CertificatePathBox.Text,
                transportProtocol: GetSelectedTransportProtocol(),
                acceptedServerCertificateSha256Fingerprint: AcceptedFingerprintBox.Text);

            var capabilities = await session.GetCapabilitiesAsync();
            var transportProtocol = GetSelectedTransportProtocol();
            shellView.ApplyCapabilities(capabilities);
            StartProjectSession(CreateCurrentProfile(), capabilities.AuthenticatedClientIdentity);
            UpdateConnectionStateText(capabilities.ProtocolVersion, capabilities.AuthenticatedClientIdentity, transportProtocol);
            StatusText.Text =
                $"Connected: protocol {capabilities.ProtocolVersion}; transport {transportProtocol}; audit {FormatAuditIdentity(capabilities.AuthenticatedClientIdentity)}";
            if (capabilities.SupportsLogStreaming)
            {
                StartLogStream();
            }
            else
            {
                UpdateLogStreamStatus("Log streaming is not supported by this endpoint.");
            }

            if (restoreDockedLiveViewOnConnect)
            {
                DockLiveView();
            }

            await RefreshSnapshotAsync();
        }
        catch (Exception ex)
        {
            shellView.ResetConnectionState();
            ClearConnectionStateText();
            StatusText.Text = $"Connection failed: {GetConnectionFailureMessage(ex)}";
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
            TransportProtocolBox.SelectedItem = RemoteControlProtocol.GrpcTransportProtocol;
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

            ApplyProfile(profile);
            StatusText.Text = "Saved connection profile loaded.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Profile load failed: {ex.Message}";
        }
    }

    private async void RefreshAdbDevicesClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "ADB device refresh started.";
            var devices = await CreateAdbClient().ListDevicesAsync();
            adbDevices.Clear();

            foreach (var device in devices)
            {
                adbDevices.Add(new AdbDeviceItem(device));
            }

            AdbDeviceBox.SelectedIndex = adbDevices.Count > 0 ? 0 : -1;
            StatusText.Text = adbDevices.Count == 0
                ? "No ADB devices or emulators were found."
                : $"ADB devices found: {adbDevices.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"ADB device refresh failed: {ex.Message}";
        }
    }

    private async void AndroidConnectClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var result = await PrepareAdbForwardFromFieldsAsync(requirePackageName: true);
            StatusText.Text =
                $"ADB forward ready on {result.Forward.Endpoint}; protocol {result.Capabilities.ProtocolVersion}; audit {FormatAuditIdentity(result.Capabilities.AuthenticatedClientIdentity)}.";
            await ConnectFromFieldsAsync(prepareAdbForward: false);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Android Connect failed: {ex.Message}";
        }
    }

    private bool ShouldPrepareAdbForwardForConnect()
    {
        return GetSelectedTransportProtocol().Equals(
                RemoteControlProtocol.AndroidBridgeTransportProtocol,
                StringComparison.OrdinalIgnoreCase) &&
            AdbDeviceBox.SelectedItem is AdbDeviceItem &&
            IsLoopbackEndpoint(EndpointBox.Text);
    }

    private async Task<AdbConnectionResult> PrepareAdbForwardFromFieldsAsync(bool requirePackageName)
    {
        if (AdbDeviceBox.SelectedItem is not AdbDeviceItem selectedDevice)
        {
            throw new InvalidOperationException("Refresh and select an ADB device before connecting to Android.");
        }

        if (!TryGetAdbHostPort(out var hostPort))
        {
            throw new InvalidOperationException("ADB host port must be a number between 1 and 65535.");
        }

        var packageName = AdbPackageBox.Text?.Trim();
        if (requirePackageName && string.IsNullOrWhiteSpace(packageName))
        {
            throw new InvalidOperationException("Enter an Android package name before Android Connect.");
        }

        var token = TokenBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(packageName) && string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException(
                "Enter a package name for marker discovery, or enter a token for explicit ADB port forwarding.");
        }

        var workflow = new AdbConnectionWorkflow(
            CreateAdbClient(),
            new GrpcRemoteControlProbe(),
            profileStore);
        var progress = new Progress<string>(message => StatusText.Text = $"ADB: {message}");
        var result = await workflow.ConnectAsync(
            new AdbConnectOptions
            {
                Serial = selectedDevice.Device.Serial,
                PackageName = string.IsNullOrWhiteSpace(packageName) ? null : packageName,
                DevicePort = string.IsNullOrWhiteSpace(packageName) ? hostPort : null,
                HostPort = hostPort,
                Token = token,
                TransportProtocol = GetSelectedTransportProtocol(),
                LaunchPackageIfStopped = !string.IsNullOrWhiteSpace(packageName),
                SaveProfile = true,
                CleanupOnExit = false,
            });

        ApplyProfile(result.ConnectionProfile);
        StatusText.Text = $"ADB forward ready on {result.Forward.Endpoint}.";
        return result;
    }

    private async void CleanupAdbForwardClicked(object? sender, RoutedEventArgs e)
    {
        if (AdbDeviceBox.SelectedItem is not AdbDeviceItem selectedDevice)
        {
            StatusText.Text = "Refresh and select an ADB device before cleanup.";
            return;
        }

        if (!TryGetAdbHostPort(out var hostPort))
        {
            return;
        }

        try
        {
            await CreateAdbClient().RemoveForwardAsync(selectedDevice.Device.Serial, hostPort);
            StopLogStream(addRow: false);
            StopDockedLiveView();
            session?.Dispose();
            session = null;
            shellView.ResetConnectionState();
            ClearConnectionStateText();
            StatusText.Text = $"ADB forward tcp:{hostPort} removed for {selectedDevice.Device.Serial}.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"ADB cleanup failed: {ex.Message}";
        }
    }

    private async void RefreshSnapshotClicked(object? sender, RoutedEventArgs e)
    {
        await RefreshSnapshotAsync();
    }

    private void OpenLiveViewClicked(object? sender, RoutedEventArgs e)
    {
        DockLiveView();
    }

    private void DockLiveView()
    {
        if (session is null)
        {
            StatusText.Text = "Connect before docking live view.";
            return;
        }

        if (dockedLiveViewControl is null)
        {
            dockedLiveViewControl = CreateRemoteViewControl();
            remoteToolsView.LiveView.Content = dockedLiveViewControl;
        }

        dockFactory.ShowLiveViewTool();
        restoreDockedLiveViewOnConnect = true;
        StatusText.Text = "Live view docked on the right.";
        ScheduleLayoutSave();
    }

    private void StopDockedLiveView()
    {
        dockedLiveViewControl?.Stop();
        remoteToolsView.LiveView.Content = null;
        dockedLiveViewControl = null;
    }

    private RemoteViewControl CreateRemoteViewControl()
    {
        var control = new RemoteViewControl(session!, liveViewCapabilities);
        control.RemoteNodeClicked += async (_, nodeId) => await SelectRemoteTreeNodeAsync(nodeId);
        control.RemoteInputSent += (_, args) => RecordLiveInput(args.Events);
        return control;
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
            lastSnapshot = snapshot;
            selectedNode = null;
            propertiesView.ShowNode(null);
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

    private async Task SelectRemoteTreeNodeAsync(string nodeId)
    {
        if (!TrySelectRemoteTreeNode(nodeId))
        {
            await RefreshSnapshotAsync();
            if (!TrySelectRemoteTreeNode(nodeId))
            {
                StatusText.Text = $"Live view selected node {nodeId}, but it is not in the current tree.";
            }
        }
    }

    private bool TrySelectRemoteTreeNode(string nodeId)
    {
        foreach (var root in treeItems)
        {
            if (FindTreeItem(root, nodeId) is { } item)
            {
                item.ExpandAncestors();
                controlTreeView.SelectedItem = item;
                ControlTreeSelectionChanged(item);
                StatusText.Text = $"Selected {item.Header} from live view.";
                return true;
            }
        }

        return false;
    }

    private static RemoteTreeItem? FindTreeItem(RemoteTreeItem item, string nodeId)
    {
        if (string.Equals(item.Node.Id, nodeId, StringComparison.Ordinal))
        {
            return item;
        }

        foreach (var child in item.Children)
        {
            if (FindTreeItem(child, nodeId) is { } found)
            {
                return found;
            }
        }

        return null;
    }

    private void ControlTreeSelectionChanged(RemoteTreeItem? item)
    {
        selectedNode = item?.Node;
        propertiesView.ShowNode(selectedNode);

        if (selectedNode is null)
        {
            return;
        }

        StatusText.Text = $"Selected {selectedNode.TypeName} {selectedNode.Name}".TrimEnd();
    }

    private void PropertySelectionChanged(PropertyRow? row)
    {
        if (row is not null)
        {
            remoteToolsView.Actions.PropertyName = row.Name;
            remoteToolsView.Actions.PropertyValue = row.Value;
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
            var nodeId = selectedNode.Id;
            var beforeArtifactId = AddSnapshotArtifact("before-click");
            var result = await session.InvokeClickAsync(nodeId);
            StatusText.Text = result.Message;
            await RefreshSnapshotAsync();
            RecordCommandInteraction(
                RemoteControlInteractionKind.Click,
                nodeId,
                string.Empty,
                string.Empty,
                result,
                beforeArtifactId);
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
            var nodeId = selectedNode.Id;
            var beforeArtifactId = AddSnapshotArtifact("before-focus");
            var result = await session.InvokeFocusAsync(nodeId);
            StatusText.Text = result.Message;
            await RefreshSnapshotAsync();
            RecordCommandInteraction(
                RemoteControlInteractionKind.Focus,
                nodeId,
                string.Empty,
                string.Empty,
                result,
                beforeArtifactId);
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

        await SetSelectedNodePropertyAsync(
            remoteToolsView.Actions.PropertyName,
            remoteToolsView.Actions.PropertyValue,
            "Property update failed");
    }

    private async void PropertyGridEditRequested(RemotePropertyEditRequestedEventArgs args)
    {
        if (session is null || selectedNode is null)
        {
            StatusText.Text = "Select a node before setting a property.";
            return;
        }

        remoteToolsView.Actions.PropertyName = args.Row.Name;
        remoteToolsView.Actions.PropertyValue = args.Row.Value;
        await SetSelectedNodePropertyAsync(args.Row.Name, args.Row.Value, "Property grid update failed");
    }

    private async Task SetSelectedNodePropertyAsync(string propertyName, string propertyValue, string failurePrefix)
    {
        if (session is null || selectedNode is null)
        {
            StatusText.Text = "Select a node before setting a property.";
            return;
        }

        try
        {
            var nodeId = selectedNode.Id;
            var beforeArtifactId = AddSnapshotArtifact("before-set-property");
            var result = await session.SetPropertyAsync(
                nodeId,
                propertyName,
                propertyValue);

            StatusText.Text = result.Message;
            await RefreshSnapshotAsync();
            RecordCommandInteraction(
                RemoteControlInteractionKind.SetProperty,
                nodeId,
                propertyName,
                propertyValue,
                result,
                beforeArtifactId);
        }
        catch (Exception ex)
        {
            StatusText.Text = $"{failurePrefix}: {ex.Message}";
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
            StopLogStream("Log stream stopped.");
            return;
        }

        StartLogStream();
    }

    private void LogViewPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!string.Equals(e.PropertyName, nameof(RemoteLogViewModel.SelectedVerbosity), StringComparison.Ordinal))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            if (session is not null && logStreamCancellation is not null)
            {
                StartLogStream();
            }
        });
    }

    private void WorkspaceTerminalPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(TerminalPanelViewModel.WorkingDirectory), StringComparison.Ordinal))
        {
            ScheduleLayoutSave();
        }
    }

    private void StartLogStream()
    {
        if (session is null)
        {
            StatusText.Text = "Connect before streaming logs.";
            return;
        }

        dockFactory.ShowLogsTool();
        StopLogStream(addRow: false);
        logStreamCancellation = new CancellationTokenSource();
        var minimumLevel = SelectedLogMinimumLevel;
        AddLogRow($"client {DateTimeOffset.UtcNow:O}: Log stream starting ({minimumLevel}).");
        UpdateLogStreamStatus($"Starting log stream ({minimumLevel}); {RemoteLogEntryCount} entries.");
        _ = WatchLogsAsync(minimumLevel, logStreamCancellation);
        StatusText.Text = $"Log stream started ({minimumLevel}).";
    }

    private void StopLogStream(string statusText = "Log stream stopped.", bool addRow = true)
    {
        var cancellation = logStreamCancellation;
        logStreamCancellation = null;
        cancellation?.Cancel();
        cancellation?.Dispose();

        if (addRow)
        {
            AddLogRow($"client {DateTimeOffset.UtcNow:O}: {statusText}");
        }

        UpdateLogStreamStatus($"{statusText} {RemoteLogEntryCount} entries.");
    }

    private async Task WatchLogsAsync(string minimumLevel, CancellationTokenSource streamCancellation)
    {
        var cancellationToken = streamCancellation.Token;

        try
        {
            await foreach (var entry in session!.WatchLogsAsync(minimumLevel, null, cancellationToken))
            {
                if (RemoteLogDisplayFormatter.IsKeepAlive(entry))
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                        UpdateLogStreamStatus($"Streaming logs ({minimumLevel}); {RemoteLogEntryCount} entries."));
                    continue;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var row = RemoteLogDisplayFormatter.Format(entry);
                    AddLogRow(row, RemoteControlProjectLogRecord.FromProtocol(entry, row));
                    UpdateLogStreamStatus($"Streaming logs ({minimumLevel}); {RemoteLogEntryCount} entries.");
                });
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ReferenceEquals(logStreamCancellation, streamCancellation))
                {
                    logStreamCancellation = null;
                    UpdateLogStreamStatus($"Log stream ended; {RemoteLogEntryCount} entries.");
                }
            });
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ReferenceEquals(logStreamCancellation, streamCancellation))
                {
                    logStreamCancellation = null;
                    var message = $"Log stream failed: {ex.Message}";
                    AddLogRow($"client {DateTimeOffset.UtcNow:O}: {message}");
                    UpdateLogStreamStatus($"{message} {RemoteLogEntryCount} entries.");
                    StatusText.Text = message;
                }
            });
        }
    }

    private string SelectedLogMinimumLevel =>
        logView.SelectedVerbosity.MinimumLevelName;

    private int RemoteLogEntryCount =>
        logView.RemoteEntryCount;

    private void AddLogRow(string row, RemoteControlProjectLogRecord? projectLog = null)
    {
        while (logView.Rows.Count >= MaxDisplayedLogRows)
        {
            logView.Rows.RemoveAt(0);
        }

        logView.Rows.Add(row);

        if (projectRecorder is not null)
        {
            if (projectLog is null)
            {
                projectRecorder.AddClientLog(row);
            }
            else
            {
                projectRecorder.Session.Logs.Add(projectLog);
                projectDocument.UpdatedUtc = DateTimeOffset.UtcNow;
            }

            _ = SaveProjectAsync();
            QueueProjectStatusUpdate();
        }
    }

    private void UpdateLogStreamStatus(string statusText)
    {
        logView.SetStatus(statusText);
        LogToggleButton.Content = logStreamCancellation is null ? "Start Logs" : "Stop Logs";
    }

    private async void SaveProjectClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            await SaveProjectAsync();
            StatusText.Text = "Project saved.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Project save failed: {ex.Message}";
        }
    }

    private void RefreshProjectClicked(object? sender, RoutedEventArgs e)
    {
        UpdateProjectStatus();
        StatusText.Text = "Project view refreshed.";
    }

    private void QueueProjectStatusUpdate()
    {
        if (isClosing)
        {
            return;
        }

        Dispatcher.UIThread.Post(UpdateProjectStatus);
    }

    private void UpdateProjectStatus()
    {
        remoteToolsView.Project.SummaryText =
            $"{projectDocument.Name} ({projectDocument.ProjectId})\n" +
            $"Profiles: {projectDocument.AppProfiles.Count}; sessions: {projectDocument.Sessions.Count}\n" +
            $"Storage: {projectStore.RootPath}";

        if (projectRecorder is null)
        {
            remoteToolsView.Project.SessionText = "No active session.";
            remoteToolsView.Project.ReplayText = "Replay data will appear after interactions are recorded.";
            return;
        }

        var active = projectRecorder.Session;
        remoteToolsView.Project.SessionText =
            $"Active session: {active.SessionId}\n" +
            $"App: {active.AppDisplayName} ({active.AppId})\n" +
            $"Transport: {active.TransportProtocol}; mode: {active.ConnectionMode}\n" +
            $"Audit identity: {FormatAuditIdentity(active.AuthenticatedClientIdentity)}\n" +
            $"Logs: {active.Logs.Count}; interactions: {active.Interactions.Count}; artifacts: {active.Artifacts.Count}";
        remoteToolsView.Project.ReplayText = active.Interactions.Count == 0
            ? "Replay ready after the first recorded interaction."
            : $"Replay journal ready: {active.Interactions.Count} steps; tree diff artifacts: {active.Artifacts.Count}.";
    }

    private void ScheduleLayoutSave()
    {
        if (isClosing || isApplyingLayoutState || !isProjectLoaded)
        {
            return;
        }

        layoutSaveTimer.Stop();
        layoutSaveTimer.Start();
    }

    private void CaptureLayoutState()
    {
        projectDocument.ClientLayout ??= new RemoteControlClientLayoutState();
        var layout = projectDocument.ClientLayout;

        var width = Bounds.Width > 0 ? Bounds.Width : Width;
        var height = Bounds.Height > 0 ? Bounds.Height : Height;

        layout.WindowWidth = Clamp(width, MinWidth, 4000);
        layout.WindowHeight = Clamp(height, MinHeight, 3000);
        layout.WindowState = WindowState.ToString();
        if (WindowState == WindowState.Normal)
        {
            layout.WindowX = Position.X;
            layout.WindowY = Position.Y;
        }

        layout.RightToolTabIndex = Math.Max(0, remoteToolsView.SelectedTabIndex);
        layout.WorkspaceTabIndex = Math.Max(0, workspaceView.SelectedTabIndex);
        layout.TerminalWorkingDirectory = workspaceView.Terminal.WorkingDirectory;
        layout.LiveViewDocked = dockedLiveViewControl is not null || restoreDockedLiveViewOnConnect;
        layout.LiveViewDockStateInitialized = true;

        // Persist the full dock tree (proportions, drag rearrangement) via Dock's serializer, keyed by project.
        // Serialization is atomic (see the store), so a failure here never corrupts the last good layout.
        if (Dock.Layout is Dock.Model.Controls.IRootDock rootDock)
        {
            try
            {
                dockLayoutStore.Save(rootDock, projectDocument.ProjectId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dock layout save failed: {ex.Message}");
            }
        }
    }

    private void ApplyLayoutState(RemoteControlClientLayoutState? layout)
    {
        if (layout is null)
        {
            shellView.ApplyLayoutState(null);
            return;
        }

        isApplyingLayoutState = true;
        try
        {
            Width = Clamp(layout.WindowWidth, MinWidth, 4000);
            Height = Clamp(layout.WindowHeight, MinHeight, 3000);

            if (layout.WindowX is { } x && layout.WindowY is { } y)
            {
                Position = new PixelPoint((int)Math.Round(x), (int)Math.Round(y));
            }

            if (Enum.TryParse<WindowState>(layout.WindowState, out var savedState)
                && savedState != WindowState.Minimized)
            {
                WindowState = savedState;
            }

            remoteToolsView.SelectedTabIndex = Math.Clamp(layout.RightToolTabIndex, 0, 1);
            workspaceView.SelectedTabIndex = Math.Clamp(layout.WorkspaceTabIndex, 0, 1);
            workspaceView.Terminal.WorkingDirectory = string.IsNullOrWhiteSpace(layout.TerminalWorkingDirectory)
                ? ToolProcessContext.StartupWorkingDirectory
                : ToolProcessContext.ResolveStartupWorkingDirectory(layout.TerminalWorkingDirectory);
            shellView.ApplyLayoutState(layout);

            if (!restoreDockedLiveViewOnConnect)
            {
                dockFactory.HideLiveViewTool();
            }
        }
        finally
        {
            isApplyingLayoutState = false;
        }
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            return minimum;
        }

        return Math.Clamp(value, minimum, maximum);
    }

    private async Task LoadProjectAsync()
    {
        try
        {
            projectDocument = await projectStore.LoadAsync(RemoteControlProjectIds.DefaultProjectId)
                ?? RemoteControlProjectDocument.Create(
                    RemoteControlProjectIds.DefaultProjectId,
                    RemoteControlProjectIds.DefaultProjectName);

            // Restore the persisted dock tree (re-attaches panel view-models from the live shell); fall back
            // to the default layout already assigned in the constructor when none exists or it fails to load.
            try
            {
                if (dockLayoutStore.Load(projectDocument.ProjectId, dockFactory) is { } savedDock)
                {
                    shellView.Layout = savedDock;
                    Dock.Layout = savedDock;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dock layout load failed: {ex.Message}");
            }

            ApplyLayoutState(projectDocument.ClientLayout);
            isProjectLoaded = true;
            await SaveProjectAsync();
            QueueProjectStatusUpdate();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Project load failed: {ex.Message}";
        }
    }

    private async Task SaveProjectAsync(bool captureLayout = true)
    {
        if (captureLayout)
        {
            CaptureLayoutState();
        }

        await projectSaveLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await projectStore.SaveAsync(projectDocument).ConfigureAwait(false);
            QueueProjectStatusUpdate();
        }
        finally
        {
            projectSaveLock.Release();
        }
    }

    private void StartProjectSession(RemoteControlConnectionProfile profile, string authenticatedClientIdentity)
    {
        projectRecorder?.Complete();
        projectRecorder = RemoteControlProjectSessionRecorder.Start(projectDocument, profile);
        projectRecorder.Session.AuthenticatedClientIdentity = FormatAuditIdentity(authenticatedClientIdentity);
        _ = SaveProjectAsync();
        QueueProjectStatusUpdate();
    }

    private string AddSnapshotArtifact(string prefix)
    {
        return projectRecorder is not null && lastSnapshot is not null
            ? projectRecorder.AddTreeSnapshotArtifact(prefix, lastSnapshot)
            : string.Empty;
    }

    private void RecordCommandInteraction(
        RemoteControlInteractionKind kind,
        string nodeId,
        string propertyName,
        string propertyValue,
        CommandResult result,
        string beforeArtifactId)
    {
        if (projectRecorder is null)
        {
            return;
        }

        var interaction = new RemoteControlInteractionRecord
        {
            Kind = kind,
            NodeId = nodeId,
            PropertyName = propertyName,
            PropertyValue = propertyValue,
            BeforeSnapshotArtifactId = beforeArtifactId,
            AfterSnapshotArtifactId = AddSnapshotArtifact($"after-{kind.ToString().ToLowerInvariant()}"),
            ResultSucceeded = result.Succeeded,
            ResultMessage = result.Message,
            TimestampUtc = DateTimeOffset.UtcNow,
            ElapsedMilliseconds = GetProjectSessionElapsedMilliseconds(),
        };

        if (kind == RemoteControlInteractionKind.SetProperty && IsSensitiveFieldName(propertyName))
        {
            interaction.SensitiveFields.Add("propertyValue");
        }

        projectRecorder.AddInteraction(interaction);
        _ = SaveProjectAsync();
        QueueProjectStatusUpdate();
    }

    private void RecordLiveInput(IReadOnlyList<RemoteInputEvent> events)
    {
        if (projectRecorder is null || events.Count == 0)
        {
            return;
        }

        var interaction = new RemoteControlInteractionRecord
        {
            Kind = RemoteControlInteractionKind.InputBatch,
            BeforeSnapshotArtifactId = AddSnapshotArtifact("before-input"),
            TimestampUtc = DateTimeOffset.UtcNow,
            ElapsedMilliseconds = GetProjectSessionElapsedMilliseconds(),
            ResultSucceeded = true,
            ResultMessage = "Input sent.",
        };

        for (var index = 0; index < events.Count; index++)
        {
            var inputEvent = events[index];
            var isSensitive = inputEvent.Kind == RemoteInputKind.Text && !string.IsNullOrEmpty(inputEvent.Text);
            interaction.InputEvents.Add(new RemoteControlInputEventRecord
            {
                Kind = inputEvent.Kind.ToString(),
                X = inputEvent.X,
                Y = inputEvent.Y,
                Button = inputEvent.Button.ToString(),
                DeltaX = inputEvent.DeltaX,
                DeltaY = inputEvent.DeltaY,
                Key = inputEvent.Key,
                Text = inputEvent.Text,
                Timestamp = inputEvent.Timestamp,
                IsSensitive = isSensitive,
            });

            if (isSensitive)
            {
                interaction.SensitiveFields.Add($"input[{index}].text");
            }
        }

        projectRecorder.AddInteraction(interaction);
        _ = SaveProjectAsync();
        QueueProjectStatusUpdate();
    }

    private long GetProjectSessionElapsedMilliseconds()
    {
        return projectRecorder is null
            ? 0
            : Math.Max(
                0,
                (long)(DateTimeOffset.UtcNow - projectRecorder.Session.StartedUtc).TotalMilliseconds);
    }

    private RemoteControlConnectionProfile CreateCurrentProfile()
    {
        var endpoint = EndpointBox.Text?.Trim() ?? string.Empty;
        var packageName = AdbPackageBox.Text?.Trim() ?? string.Empty;
        var selectedDevice = AdbDeviceBox.SelectedItem as AdbDeviceItem;
        var transportProtocol = GetSelectedTransportProtocol();
        var connectionMode = GetProjectConnectionMode(endpoint, packageName, transportProtocol);
        var displayName = !string.IsNullOrWhiteSpace(packageName)
            ? packageName
            : endpoint;

        return new RemoteControlConnectionProfile
        {
            AppId = packageName,
            DisplayName = displayName,
            Endpoint = endpoint,
            Token = TokenBox.Text ?? string.Empty,
            CertificatePath = CertificatePathBox.Text ?? string.Empty,
            AcceptedServerCertificateSha256Fingerprint = AcceptedFingerprintBox.Text ?? string.Empty,
            TransportProtocol = transportProtocol,
            ConnectionMode = connectionMode,
            AndroidPackageName = packageName,
            AndroidSerial = selectedDevice?.Device.Serial ?? string.Empty,
            AdbHostPort = TryParseNullablePort(AdbHostPortBox.Text),
            UpdatedUtc = DateTimeOffset.UtcNow,
        };
    }

    private static string GetProjectConnectionMode(
        string endpoint,
        string packageName,
        string transportProtocol)
    {
        if (!string.IsNullOrWhiteSpace(packageName) ||
            transportProtocol.Equals(RemoteControlProtocol.AndroidBridgeTransportProtocol, StringComparison.OrdinalIgnoreCase))
        {
            return "adb";
        }

        if (Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase)))
        {
            return "local";
        }

        return "network";
    }

    private static bool IsLoopbackEndpoint(string? endpoint)
    {
        return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
            (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase));
    }

    private string GetConnectionFailureMessage(Exception exception)
    {
        if (GetSelectedTransportProtocol().Equals(
                RemoteControlProtocol.AndroidBridgeTransportProtocol,
                StringComparison.OrdinalIgnoreCase) &&
            IsLoopbackEndpoint(EndpointBox.Text) &&
            exception.Message.Contains("actively refused", StringComparison.OrdinalIgnoreCase))
        {
            return "ADB bridge endpoint is not reachable. Refresh and select the device, then use Android Connect with a package name, or use Connect with a selected device, token, and matching ADB host/device port.";
        }

        return exception.Message;
    }

    private void UpdateConnectionStateText(
        string protocolVersion,
        string authenticatedClientIdentity,
        string transportProtocol)
    {
        ConnectionStateText.Text =
            $"Transport: {transportProtocol}; protocol: {protocolVersion}; audit: {FormatAuditIdentity(authenticatedClientIdentity)}";
    }

    private void ClearConnectionStateText()
    {
        ConnectionStateText.Text = "Transport: -; audit: -";
    }

    private static string FormatAuditIdentity(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
    }

    private static int? TryParseNullablePort(string? value)
    {
        return int.TryParse(value, out var port) && port is >= 1 and <= 65535
            ? port
            : null;
    }

    private static bool IsSensitiveFieldName(string value)
    {
        var text = value.ToLowerInvariant();
        return text.Contains("password", StringComparison.Ordinal) ||
            text.Contains("token", StringComparison.Ordinal) ||
            text.Contains("secret", StringComparison.Ordinal) ||
            text.Contains("key", StringComparison.Ordinal) ||
            text.Contains("credential", StringComparison.Ordinal) ||
            text.Contains("auth", StringComparison.Ordinal) ||
            text.Contains("cookie", StringComparison.Ordinal) ||
            text.Contains("connectionstring", StringComparison.Ordinal) ||
            text.Contains("connection string", StringComparison.Ordinal);
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
        var profile = CreateCurrentProfile();
        await profileStore.SaveDefaultAsync(profile);
        projectDocument.UpsertAppProfile(profile);
        await SaveProjectAsync();

        StatusText.Text = statusText;
    }

    private void ApplyProfile(RemoteControlConnectionProfile profile)
    {
        EndpointBox.Text = profile.Endpoint;
        TokenBox.Text = profile.Token;
        CertificatePathBox.Text = profile.CertificatePath;
        AcceptedFingerprintBox.Text = profile.AcceptedServerCertificateSha256Fingerprint;
        TransportProtocolBox.SelectedItem = profile.TransportProtocol;
        if (!string.IsNullOrWhiteSpace(profile.AndroidPackageName))
        {
            AdbPackageBox.Text = profile.AndroidPackageName;
        }

        if (profile.AdbHostPort is > 0)
        {
            AdbHostPortBox.Text = profile.AdbHostPort.Value.ToString();
        }

        UpdateTerminalMcpProfileFromFields();
    }

    private string GetSelectedTransportProtocol()
    {
        return TransportProtocolBox.SelectedItem as string
            ?? RemoteControlProtocol.GrpcTransportProtocol;
    }

    private void UpdateTerminalMcpProfileFromFields()
    {
        workspaceView.Terminal.RemoteControlEndpoint = EndpointBox.Text ?? string.Empty;
        workspaceView.Terminal.RemoteControlToken = TokenBox.Text ?? string.Empty;
        workspaceView.Terminal.RemoteControlTransportProtocol = GetSelectedTransportProtocol();
        workspaceView.Terminal.RemoteControlCertificatePath = CertificatePathBox.Text ?? string.Empty;
        workspaceView.Terminal.RemoteControlAcceptedFingerprint = AcceptedFingerprintBox.Text ?? string.Empty;
        if (mcpHostController?.Endpoint is { } endpoint)
        {
            workspaceView.Terminal.RemoteControlMcpUrl = endpoint.ToString();
        }
    }

    private RemoteControlMcpOptions CreateMcpOptionsFromTerminalState()
    {
        var terminal = workspaceView.Terminal;
        var endpointText = string.IsNullOrWhiteSpace(terminal.RemoteControlEndpoint)
            ? RemoteControlMcpOptions.DefaultEndpoint.ToString()
            : terminal.RemoteControlEndpoint.Trim();

        if (!Uri.TryCreate(endpointText, UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException("Remote-control endpoint must be an absolute URI.");
        }

        if (string.IsNullOrWhiteSpace(terminal.RemoteControlToken))
        {
            throw new InvalidOperationException("Remote-control bearer token is required before Codex can drive the target.");
        }

        return RemoteControlMcpOptions.Create(
            endpoint,
            terminal.RemoteControlToken.Trim(),
            string.IsNullOrWhiteSpace(terminal.RemoteControlTransportProtocol)
                ? RemoteControlProtocol.GrpcTransportProtocol
                : terminal.RemoteControlTransportProtocol.Trim(),
            terminal.RemoteControlCertificatePath,
            terminal.RemoteControlAcceptedFingerprint);
    }

    private AdbClient CreateAdbClient()
    {
        var adbPath = AdbPathBox.Text;
        return new AdbClient(new ProcessAdbCommandRunner(
            string.IsNullOrWhiteSpace(adbPath) ? null : adbPath.Trim()));
    }

    private bool TryGetAdbHostPort(out int hostPort)
    {
        if (int.TryParse(AdbHostPortBox.Text, out hostPort) && hostPort is >= 1 and <= 65535)
        {
            return true;
        }

        StatusText.Text = "ADB host port must be a number between 1 and 65535.";
        return false;
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
                parent.AddChild(item);
            }
        }

        return roots;
    }
}

/// <summary>
/// View model for a remote control tree node.
/// </summary>
public sealed class RemoteTreeItem : INotifyPropertyChanged
{
    private bool isExpanded;

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

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the protocol tree node.
    /// </summary>
    public TreeNode Node { get; }

    /// <summary>
    /// Gets the parent tree item, when the node is not a root.
    /// </summary>
    public RemoteTreeItem? Parent { get; private set; }

    /// <summary>
    /// Gets the display header.
    /// </summary>
    public string Header { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this item is expanded in the tree panel.
    /// </summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set
        {
            if (isExpanded == value)
            {
                return;
            }

            isExpanded = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets child tree items.
    /// </summary>
    public ObservableCollection<RemoteTreeItem> Children { get; } = [];

    /// <summary>
    /// Adds a child item and records the parent relationship used to reveal live-view selections.
    /// </summary>
    /// <param name="child">Child item.</param>
    public void AddChild(RemoteTreeItem child)
    {
        ArgumentNullException.ThrowIfNull(child);
        child.Parent = this;
        Children.Add(child);
    }

    /// <summary>
    /// Expands every ancestor so this item is visible when selected programmatically.
    /// </summary>
    public void ExpandAncestors()
    {
        var current = Parent;
        while (current is not null)
        {
            current.IsExpanded = true;
            current = current.Parent;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// View model for a selected node property.
/// </summary>
/// <param name="Name">Property name.</param>
/// <param name="Value">Current value.</param>
/// <param name="Display">Display text.</param>
/// <param name="DeclaringType">Remote declaring type.</param>
/// <param name="ValueType">Remote value type.</param>
/// <param name="CanWrite">Whether the remote runtime reports the property as writable.</param>
/// <param name="IsRedacted">Whether the remote runtime redacted the property value.</param>
public sealed record PropertyRow(
    string Name,
    string Value,
    string Display,
    string DeclaringType = "",
    string ValueType = "",
    bool CanWrite = false,
    bool IsRedacted = false)
{
    /// <inheritdoc />
    public override string ToString()
    {
        return Display;
    }
}

internal sealed class AdbDeviceItem
{
    public AdbDeviceItem(AdbDevice device)
    {
        Device = device;
    }

    public AdbDevice Device { get; }

    public override string ToString()
    {
        var name = Device.Model ?? Device.Device ?? Device.Product ?? "Android";
        return $"{Device.Serial} {name} ({Device.State})";
    }
}
