using Avalonia;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Avalonia.Threading;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Main desktop client window for Avalonia.RemoteControl.
/// </summary>
public sealed partial class MainWindow : Window
{
    private const int MaxDisplayedLogRows = 2000;
    private readonly ControlTreePanelViewModel controlTreeView = new();
    private readonly WorkspacePanelViewModel workspaceView = new();
    private readonly RemoteToolsPanelViewModel remoteToolsView = new();
    private readonly RemoteLogViewModel logView = new();
    private readonly ObservableCollection<AdbDeviceItem> adbDevices = [];
    private readonly IRemoteControlProfileStore profileStore = new FileRemoteControlProfileStore();
    private readonly FileRemoteControlProjectStore projectStore = new();
    private readonly SemaphoreSlim projectSaveLock = new(1, 1);
    private readonly DispatcherTimer layoutSaveTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    private RemoteControlProjectDocument projectDocument =
        RemoteControlProjectDocument.Create(RemoteControlProjectIds.DefaultProjectId, RemoteControlProjectIds.DefaultProjectName);
    private RemoteControlProjectSessionRecorder? projectRecorder;
    private RemoteControlDesktopSession? session;
    private RemoteLiveViewCapabilities liveViewCapabilities = RemoteLiveViewCapabilities.None;
    private TreeNode? selectedNode;
    private TreeSnapshot? lastSnapshot;
    private CancellationTokenSource? logStreamCancellation;
    private RemoteControlServerCertificateInfo? pendingCertificateInfo;
    private readonly Dictionary<string, FloatingDockPaneWindow> floatingToolWindows = [];
    private LogPanel? floatingLogPanel;
    private RemoteViewControl? floatingLiveViewControl;
    private RemoteViewControl? dockedLiveViewControl;
    private bool isClosing;
    private bool isApplyingLayoutState;
    private bool isProjectLoaded;
    private bool restoreFloatingLogOnOpen;
    private bool restoreDockedLiveViewOnConnect;

    private ObservableCollection<RemoteTreeItem> treeItems => controlTreeView.Items;

    private PropertiesPanelViewModel propertiesView => workspaceView.Properties;

    private ObservableCollection<PropertyRow> propertyRows => propertiesView.Rows;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        ControlTreePanel.ViewModel = controlTreeView;
        ControlTreePanel.SelectedItemChanged += (_, item) => ControlTreeSelectionChanged(item);
        WorkspacePanel.ViewModel = workspaceView;
        WorkspacePanel.SelectedTabChanged += (_, _) => ScheduleLayoutSave();
        WorkspacePanel.Properties.PropertySelected += (_, row) => PropertySelectionChanged(row);
        RemoteToolsPanel.ViewModel = remoteToolsView;
        RemoteToolsPanel.SelectedTabChanged += (_, _) => ScheduleLayoutSave();
        RemoteToolsPanel.LiveViewCommandRequested += DockPaneCommandRequested;
        RemoteToolsPanel.LiveViewHeaderDragCompleted += DockPaneHeaderDragCompleted;
        RemoteToolsPanel.Actions.InvokeClickRequested += (_, _) => InvokeClickClicked(null, new RoutedEventArgs());
        RemoteToolsPanel.Actions.FocusRequested += (_, _) => InvokeFocusClicked(null, new RoutedEventArgs());
        RemoteToolsPanel.Actions.SetPropertyRequested += (_, _) => SetPropertyClicked(null, new RoutedEventArgs());
        RemoteToolsPanel.Project.SaveProjectRequested += (_, _) => SaveProjectClicked(null, new RoutedEventArgs());
        RemoteToolsPanel.Project.RefreshRequested += (_, _) => RefreshProjectClicked(null, new RoutedEventArgs());
        DockedLogPanel.ViewModel = logView;
        DockedLogPanel.FloatRequested += (_, _) => ShowLogToolWindow("Log panel floated.");
        DockedLogPanel.DockRequested += (_, _) => DockLogsToMain("Log panel docked.");
        logView.PropertyChanged += LogViewPropertyChanged;
        AdbDeviceBox.ItemsSource = adbDevices;
        TransportProtocolBox.ItemsSource = new[]
        {
            RemoteControlProtocol.GrpcTransportProtocol,
            RemoteControlProtocol.AndroidBridgeTransportProtocol,
        };
        TransportProtocolBox.SelectedItem = RemoteControlProtocol.GrpcTransportProtocol;
        AdbPathBox.Text = ProcessAdbCommandRunner.ResolveDefaultAdbPath();
        SizeChanged += (_, _) => ScheduleLayoutSave();
        Opened += (_, _) => RestoreFloatingLogIfNeeded();
        layoutSaveTimer.Tick += (_, _) =>
        {
            layoutSaveTimer.Stop();
            _ = SaveProjectAsync();
        };
        UpdateLogStreamStatus("Log stream stopped.");
        UpdateLogPresentationState();
        UpdateProjectStatus();

        Closing += (_, _) =>
        {
            isClosing = true;
            layoutSaveTimer.Stop();
            CaptureLayoutState();
            StopLogStream(addRow: false);
            StopDockedLiveView();
            projectRecorder?.Complete();
            CloseFloatingToolWindows();
            session?.Dispose();
            _ = SaveProjectAsync(captureLayout: false);
        };

        _ = LoadProjectAsync();
        _ = LoadProfileAsync();
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
            liveViewCapabilities = RemoteLiveViewCapabilities.FromProtocol(capabilities);
            StartProjectSession(CreateCurrentProfile());
            StatusText.Text = $"Connected: protocol {capabilities.ProtocolVersion}; transport {GetSelectedTransportProtocol()}";
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
            liveViewCapabilities = RemoteLiveViewCapabilities.None;
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
                $"ADB forward ready on {result.Forward.Endpoint}; protocol {result.Capabilities.ProtocolVersion}.";
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
            liveViewCapabilities = RemoteLiveViewCapabilities.None;
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
        _ = ShowLiveViewWindow("Live view opened.");
    }

    private bool ShowLiveViewWindow(string statusText)
    {
        if (session is null)
        {
            StatusText.Text = "Connect before opening live view.";
            return false;
        }

        if (floatingToolWindows.TryGetValue("liveView", out var existing))
        {
            existing.Activate();
            StatusText.Text = statusText;
            return true;
        }

        StopDockedLiveView();
        floatingLiveViewControl = CreateRemoteViewControl();
        var viewModel = new LiveViewPanelViewModel
        {
            Content = floatingLiveViewControl,
            PlaceholderText = "Live view is floating.",
        };
        var panel = new LiveViewPanel
        {
            ViewModel = viewModel,
        };
        var window = CreateFloatingToolWindow("liveView", "Live View", "\uE8A7", panel);
        window.Closed += FloatingLiveViewClosed;
        window.Show(this);
        restoreDockedLiveViewOnConnect = false;
        StatusText.Text = statusText;
        ScheduleLayoutSave();
        return true;
    }

    private void DockLiveViewClicked(object? sender, RoutedEventArgs e)
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

        if (dockedLiveViewControl is not null)
        {
            StatusText.Text = "Live view is already docked.";
            return;
        }

        CloseFloatingToolWindow("liveView");

        dockedLiveViewControl = CreateRemoteViewControl();
        remoteToolsView.LiveView.Content = dockedLiveViewControl;
        RemoteToolsPanel.SelectedTabIndex = 1;
        restoreDockedLiveViewOnConnect = true;
        StatusText.Text = "Live view docked on the right.";
        ScheduleLayoutSave();
    }

    private void CloseDockedLiveViewClicked(object? sender, RoutedEventArgs e)
    {
        if (dockedLiveViewControl is null)
        {
            StatusText.Text = "Live view is not docked.";
            return;
        }

        StopDockedLiveView();
        restoreDockedLiveViewOnConnect = false;
        _ = ShowLiveViewWindow("Live view undocked.");
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

    private void FloatingLiveViewClosed(object? sender, EventArgs e)
    {
        floatingLiveViewControl?.Stop();
        floatingLiveViewControl = null;
        floatingToolWindows.Remove("liveView");
        restoreDockedLiveViewOnConnect = false;
        ScheduleLayoutSave();
    }

    private FloatingDockPaneWindow CreateFloatingToolWindow(
        string panelId,
        string title,
        string glyph,
        Control content)
    {
        var window = new FloatingDockPaneWindow(panelId, title, glyph, content);
        floatingToolWindows[panelId] = window;
        window.CommandRequested += FloatingToolWindowCommandRequested;
        window.Closed += (_, _) =>
        {
            floatingToolWindows.Remove(panelId);
            if (panelId is not ("logs" or "liveView"))
            {
                ShowDockPane(panelId);
            }

            ScheduleLayoutSave();
        };

        return window;
    }

    private void CloseFloatingToolWindow(string panelId)
    {
        if (!floatingToolWindows.Remove(panelId, out var window))
        {
            return;
        }

        window.Close();
    }

    private void CloseFloatingToolWindows()
    {
        foreach (var panelId in floatingToolWindows.Keys.ToArray())
        {
            CloseFloatingToolWindow(panelId);
        }
    }

    private void FloatingToolWindowCommandRequested(object? sender, DockPaneCommandEventArgs e)
    {
        switch (e.Command)
        {
            case DockPaneCommand.Dock:
            case DockPaneCommand.Restore:
                DockToolPanel(e.PanelId);
                break;
            case DockPaneCommand.Close:
                CloseFloatingToolWindow(e.PanelId);
                break;
            case DockPaneCommand.AutoHide:
                DockToolPanel(e.PanelId);
                break;
            case DockPaneCommand.Float:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Command));
        }
    }

    private void DockPaneCommandRequested(object? sender, DockPaneCommandEventArgs e)
    {
        switch (e.Command)
        {
            case DockPaneCommand.Float:
                FloatToolPanel(e.PanelId);
                break;
            case DockPaneCommand.Dock:
            case DockPaneCommand.Restore:
                DockToolPanel(e.PanelId);
                break;
            case DockPaneCommand.AutoHide:
                StatusText.Text = $"{GetPanelTitle(e.PanelId)} auto-hide toggled.";
                ScheduleLayoutSave();
                break;
            case DockPaneCommand.Close:
                StatusText.Text = $"{GetPanelTitle(e.PanelId)} hidden.";
                ScheduleLayoutSave();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Command));
        }
    }

    private void DockPaneHeaderDragCompleted(object? sender, DockPaneDragCompletedEventArgs e)
    {
        FloatToolPanel(e.PanelId);
    }

    private void FloatToolPanel(string panelId)
    {
        if (floatingToolWindows.TryGetValue(panelId, out var existing))
        {
            existing.Activate();
            HideDockPane(panelId);
            return;
        }

        switch (panelId)
        {
            case "logs":
                ShowLogToolWindow("Log panel floated.");
                return;
            case "liveView":
                ShowLiveViewWindow("Live view floated.");
                return;
        }

        var content = CreatePanelContent(panelId, floating: true);
        if (content is null)
        {
            StatusText.Text = $"Panel {panelId} cannot be floated.";
            return;
        }

        HideDockPane(panelId);

        var window = CreateFloatingToolWindow(panelId, GetPanelTitle(panelId), GetPanelGlyph(panelId), content);
        window.Show(this);
        StatusText.Text = $"{GetPanelTitle(panelId)} floated.";
        ScheduleLayoutSave();
    }

    private void DockToolPanel(string panelId)
    {
        switch (panelId)
        {
            case "logs":
                DockLogsToMain("Log panel docked.");
                return;
            case "liveView":
                DockLiveView();
                return;
        }

        CloseFloatingToolWindow(panelId);
        ShowDockPane(panelId);

        StatusText.Text = $"{GetPanelTitle(panelId)} docked.";
        ScheduleLayoutSave();
    }

    private void HideDockPane(string panelId)
    {
        SetDockPaneVisibility(panelId, isVisible: false);
    }

    private void ShowDockPane(string panelId)
    {
        SetDockPaneVisibility(panelId, isVisible: true);
    }

    private void SetDockPaneVisibility(string panelId, bool isVisible)
    {
        if (GetDockPane(panelId) is not { } pane)
        {
            return;
        }

        if (isVisible)
        {
            pane.IsAutoHidden = false;
        }

        if (pane.IsVisible == isVisible)
        {
            return;
        }

        pane.IsVisible = isVisible;
        WorkspaceDockLayout.InvalidateMeasure();
        WorkspaceDockLayout.InvalidateArrange();
    }

    private Control? CreatePanelContent(string panelId, bool floating)
    {
        return panelId switch
        {
            "controlTree" => CreateControlTreePanel(),
            "properties" => CreatePropertiesPanel(),
            "workspace" => CreateWorkspacePanel(),
            "remoteTools" => CreateRemoteToolsPanel(floating),
            _ => null,
        };
    }

    private ControlTreePanel CreateControlTreePanel()
    {
        var panel = new ControlTreePanel
        {
            ViewModel = controlTreeView,
        };
        panel.SelectedItemChanged += (_, item) => ControlTreeSelectionChanged(item);
        return panel;
    }

    private PropertiesPanel CreatePropertiesPanel()
    {
        var panel = new PropertiesPanel
        {
            ViewModel = propertiesView,
        };
        panel.PropertySelected += (_, row) => PropertySelectionChanged(row);
        return panel;
    }

    private WorkspacePanel CreateWorkspacePanel()
    {
        var panel = new WorkspacePanel
        {
            ViewModel = workspaceView,
        };
        panel.SelectedTabChanged += (_, _) => ScheduleLayoutSave();
        panel.Properties.PropertySelected += (_, row) => PropertySelectionChanged(row);
        return panel;
    }

    private RemoteToolsPanel CreateRemoteToolsPanel(bool floating)
    {
        if (floating)
        {
            StopDockedLiveView();
        }

        var panel = new RemoteToolsPanel
        {
            ViewModel = remoteToolsView,
        };
        WireRemoteToolsPanel(panel);
        return panel;
    }

    private void WireRemoteToolsPanel(RemoteToolsPanel panel)
    {
        panel.SelectedTabChanged += (_, _) => ScheduleLayoutSave();
        panel.LiveViewCommandRequested += DockPaneCommandRequested;
        panel.LiveViewHeaderDragCompleted += DockPaneHeaderDragCompleted;
        panel.Actions.InvokeClickRequested += (_, _) => InvokeClickClicked(null, new RoutedEventArgs());
        panel.Actions.FocusRequested += (_, _) => InvokeFocusClicked(null, new RoutedEventArgs());
        panel.Actions.SetPropertyRequested += (_, _) => SetPropertyClicked(null, new RoutedEventArgs());
        panel.Project.SaveProjectRequested += (_, _) => SaveProjectClicked(null, new RoutedEventArgs());
        panel.Project.RefreshRequested += (_, _) => RefreshProjectClicked(null, new RoutedEventArgs());
    }

    private DockPaneChrome? GetDockPane(string panelId)
    {
        return panelId switch
        {
            "controlTree" => ControlTreePane,
            "workspace" => WorkspacePane,
            "remoteTools" => RemoteToolsPane,
            "logs" => LogsPane,
            _ => null,
        };
    }

    private static string GetPanelTitle(string panelId)
    {
        return panelId switch
        {
            "controlTree" => "Control Tree",
            "properties" => "Properties",
            "workspace" => "Workspace",
            "remoteTools" => "Remote Tools",
            "logs" => "Logs",
            "liveView" => "Live View",
            _ => panelId,
        };
    }

    private static string GetPanelGlyph(string panelId)
    {
        return panelId switch
        {
            "controlTree" => "\uE8B7",
            "properties" => "\uE8EC",
            "workspace" => "\uE756",
            "remoteTools" => "\uE713",
            "logs" => "\uE8A5",
            "liveView" => "\uE8A7",
            _ => "\uE8B7",
        };
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
                ControlTreePanel.SelectItem(item);
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

        try
        {
            var nodeId = selectedNode.Id;
            var propertyName = remoteToolsView.Actions.PropertyName;
            var propertyValue = remoteToolsView.Actions.PropertyValue;
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
            StopLogStream("Log stream stopped.");
            return;
        }

        StartLogStream();
    }

    private void PopOutLogsClicked(object? sender, RoutedEventArgs e)
    {
        ShowLogToolWindow("Log panel floated.");
    }

    private bool ShowLogToolWindow(string statusText)
    {
        if (floatingToolWindows.TryGetValue("logs", out var existing))
        {
            existing.Activate();
            HideDockPane("logs");
            StatusText.Text = statusText;
            return true;
        }

        floatingLogPanel = new LogPanel
        {
            ViewModel = logView,
            IsDockHost = false,
        };
        floatingLogPanel.DockRequested += (_, _) => DockLogsToMain("Log panel docked.");

        var window = CreateFloatingToolWindow("logs", "Logs", "\uE8A5", floatingLogPanel);
        window.Closed += FloatingLogToolWindowClosed;
        logView.PopOut();
        UpdateLogPresentationState();
        HideDockPane("logs");
        window.Show(this);
        StatusText.Text = statusText;
        ScheduleLayoutSave();
        return true;
    }

    private void FloatingLogToolWindowClosed(object? sender, EventArgs e)
    {
        floatingToolWindows.Remove("logs");
        floatingLogPanel = null;

        if (logView.IsPoppedOut)
        {
            restoreFloatingLogOnOpen = false;
            logView.Dock();
            UpdateLogPresentationState();
            ShowDockPane("logs");
            StatusText.Text = "Log window closed.";
            ScheduleLayoutSave();
        }
    }

    private void DockLogsToMain(string statusText)
    {
        restoreFloatingLogOnOpen = false;
        logView.Dock();
        UpdateLogPresentationState();
        ShowDockPane("logs");
        CloseFloatingToolWindow("logs");

        StatusText.Text = statusText;
        ScheduleLayoutSave();
    }

    private void UpdateLogPresentationState()
    {
        DockedLogPanel.ViewModel = logView;
    }

    private void DockLogsClicked(object? sender, RoutedEventArgs e)
    {
        DockLogsToMain("Log window docked.");
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

    private void StartLogStream()
    {
        if (session is null)
        {
            StatusText.Text = "Connect before streaming logs.";
            return;
        }

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
        if (!logView.IsPoppedOut)
        {
            DockedLogPanel.ScrollToEnd();
        }

        floatingLogPanel?.ScrollToEnd();

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

        layout.TreePaneWidth = WorkspaceDockLayout.WestWidth;
        layout.RightPaneWidth = WorkspaceDockLayout.EastWidth;
        layout.LogPaneHeight = WorkspaceDockLayout.SouthHeight;
        layout.RightToolTabIndex = Math.Max(0, RemoteToolsPanel.SelectedTabIndex);
        layout.WorkspaceTabIndex = Math.Max(0, WorkspacePanel.SelectedTabIndex);
        layout.LogsPoppedOut = logView.IsPoppedOut;
        layout.LiveViewDocked = dockedLiveViewControl is not null || restoreDockedLiveViewOnConnect;
        layout.ControlTreeAutoHidden = ControlTreePane.IsAutoHidden;
        layout.PropertiesAutoHidden = WorkspacePane.IsAutoHidden;
        layout.RemoteToolsAutoHidden = RemoteToolsPane.IsAutoHidden;
        layout.LogsAutoHidden = LogsPane.IsAutoHidden;
    }

    private void ApplyLayoutState(RemoteControlClientLayoutState? layout)
    {
        if (layout is null)
        {
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

            WorkspaceDockLayout.WestWidth = Clamp(layout.TreePaneWidth, 220, 800);
            WorkspaceDockLayout.EastWidth = Clamp(layout.RightPaneWidth, 300, 900);
            WorkspaceDockLayout.SouthHeight = Clamp(layout.LogPaneHeight, 120, 500);
            RemoteToolsPanel.SelectedTabIndex = Math.Clamp(layout.RightToolTabIndex, 0, 2);
            WorkspacePanel.SelectedTabIndex = Math.Clamp(layout.WorkspaceTabIndex, 0, 1);
            restoreDockedLiveViewOnConnect = layout.LiveViewDocked;
            ControlTreePane.IsAutoHidden = layout.ControlTreeAutoHidden;
            WorkspacePane.IsAutoHidden = layout.PropertiesAutoHidden;
            RemoteToolsPane.IsAutoHidden = layout.RemoteToolsAutoHidden;
            LogsPane.IsAutoHidden = layout.LogsAutoHidden;

            if (layout.LogsPoppedOut)
            {
                restoreFloatingLogOnOpen = true;
                Dispatcher.UIThread.Post(RestoreFloatingLogIfNeeded);
            }
        }
        finally
        {
            isApplyingLayoutState = false;
        }
    }

    private void RestoreFloatingLogIfNeeded()
    {
        if (!restoreFloatingLogOnOpen || !IsVisible)
        {
            return;
        }

        restoreFloatingLogOnOpen = false;
        _ = ShowLogToolWindow("Log panel restored.");
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

    private void StartProjectSession(RemoteControlConnectionProfile profile)
    {
        projectRecorder?.Complete();
        projectRecorder = RemoteControlProjectSessionRecorder.Start(projectDocument, profile);
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
    }

    private string GetSelectedTransportProtocol()
    {
        return TransportProtocolBox.SelectedItem as string
            ?? RemoteControlProtocol.GrpcTransportProtocol;
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
