using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace Avalonia.RemoteControl.Tool.Docking;

/// <summary>
/// Builds and initializes the Dock.Avalonia layout for the remote-control tool shell.
/// </summary>
public sealed class RemoteControlDockFactory : Factory
{
    private readonly RemoteControlToolShellViewModel _shell;

    private LiveViewDockable? _liveView;
    private IToolDock? _liveViewDock;
    private LogsDockable? _logs;
    private IToolDock? _logsDock;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlDockFactory"/> class.
    /// </summary>
    /// <param name="shell">Shell view-model providing panel state.</param>
    public RemoteControlDockFactory(RemoteControlToolShellViewModel shell)
    {
        _shell = shell ?? throw new ArgumentNullException(nameof(shell));
    }

    /// <inheritdoc />
    public override IRootDock CreateLayout()
    {
        var controlTree = new ControlTreeDockable(_shell.ControlTree);
        var remoteTools = new RemoteToolsDockable(_shell.RemoteTools);
        var liveView = new LiveViewDockable(_shell.RemoteTools.LiveView);
        var logs = new LogsDockable(_shell.Logs);
        var workspace = new WorkspaceDockable(_shell.Workspace);

        // West: control tree (legacy WestWidth 340 ≈ 0.29 of 1180).
        var controlTreeDock = CreateToolDock();
        controlTreeDock.Id = "controlTreeDock";
        controlTreeDock.Alignment = Alignment.Left;
        controlTreeDock.Proportion = 0.29;
        controlTreeDock.ActiveDockable = controlTree;
        controlTreeDock.VisibleDockables = CreateList<IDockable>(controlTree);

        // Center-fill: workspace document well over the logs tool (legacy SouthHeight 220 ≈ 0.35 of the center column).
        var workspaceDock = CreateDocumentDock();
        workspaceDock.Id = "workspaceDock";
        workspaceDock.IsCollapsable = false;
        workspaceDock.Proportion = 0.65;
        workspaceDock.ActiveDockable = workspace;
        workspaceDock.VisibleDockables = CreateList<IDockable>(workspace);

        var logsDock = CreateToolDock();
        logsDock.Id = "logsDock";
        logsDock.Alignment = Alignment.Bottom;
        logsDock.Proportion = 0.35;
        logsDock.ActiveDockable = logs;
        logsDock.VisibleDockables = CreateList<IDockable>(logs);

        var centerDock = CreateProportionalDock();
        centerDock.Id = "centerDock";
        centerDock.Orientation = Orientation.Vertical;
        centerDock.Proportion = 0.39;
        centerDock.VisibleDockables = CreateList<IDockable>(
            workspaceDock,
            CreateProportionalDockSplitter(),
            logsDock);

        // East: remote tools over live view (legacy RightDock SouthHeight 360 ≈ 0.58 of the east column).
        var remoteToolsDock = CreateToolDock();
        remoteToolsDock.Id = "remoteToolsDock";
        remoteToolsDock.Alignment = Alignment.Right;
        remoteToolsDock.Proportion = 0.42;
        remoteToolsDock.ActiveDockable = remoteTools;
        remoteToolsDock.VisibleDockables = CreateList<IDockable>(remoteTools);

        var liveViewDock = CreateToolDock();
        liveViewDock.Id = "liveViewDock";
        liveViewDock.Alignment = Alignment.Right;
        liveViewDock.Proportion = 0.58;
        liveViewDock.ActiveDockable = liveView;
        liveViewDock.VisibleDockables = CreateList<IDockable>(liveView);

        var eastDock = CreateProportionalDock();
        eastDock.Id = "eastDock";
        eastDock.Orientation = Orientation.Vertical;
        eastDock.Proportion = 0.32;
        eastDock.VisibleDockables = CreateList<IDockable>(
            remoteToolsDock,
            CreateProportionalDockSplitter(),
            liveViewDock);

        var main = CreateProportionalDock();
        main.Id = "mainLayout";
        main.Orientation = Orientation.Horizontal;
        main.VisibleDockables = CreateList<IDockable>(
            controlTreeDock,
            CreateProportionalDockSplitter(),
            centerDock,
            CreateProportionalDockSplitter(),
            eastDock);

        var root = CreateRootDock();
        root.Id = "root";
        root.Title = "Root";
        root.IsCollapsable = false;
        root.VisibleDockables = CreateList<IDockable>(main);
        root.ActiveDockable = main;
        root.DefaultDockable = main;

        _liveView = liveView;
        _liveViewDock = liveViewDock;
        _logs = logs;
        _logsDock = logsDock;
        return root;
    }

    /// <summary>
    /// Ensures the live-view tool is docked and active in its east tool dock.
    /// </summary>
    public void ShowLiveViewTool() => ShowTool(_liveViewDock, _liveView);

    /// <summary>
    /// Removes the live-view tool from its dock (kept for redock).
    /// </summary>
    public void HideLiveViewTool() => HideTool(_liveViewDock, _liveView);

    /// <summary>
    /// Ensures the logs tool is docked and active in its south tool dock.
    /// </summary>
    public void ShowLogsTool() => ShowTool(_logsDock, _logs);

    /// <summary>
    /// Removes the logs tool from its dock (kept for redock).
    /// </summary>
    public void HideLogsTool() => HideTool(_logsDock, _logs);

    private void ShowTool(IToolDock? dock, IDockable? tool)
    {
        if (dock is null || tool is null)
        {
            return;
        }

        if (dock.VisibleDockables?.Contains(tool) != true)
        {
            AddDockable(dock, tool);
        }

        SetActiveDockable(tool);
    }

    private void HideTool(IToolDock? dock, IDockable? tool)
    {
        if (dock is null || tool is null)
        {
            return;
        }

        if (dock.VisibleDockables?.Contains(tool) == true)
        {
            RemoveDockable(tool, false);
        }
    }

    /// <inheritdoc />
    public override void InitLayout(IDockable layout)
    {
        ContextLocator = new Dictionary<string, Func<object?>>();
        DockableLocator = new Dictionary<string, Func<IDockable?>>();
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow(),
        };

        base.InitLayout(layout);

        // Re-attach panel view-models from the live shell (fresh layout: same instances; loaded layout:
        // reconnects the deserialized structure to the running shell).
        ReattachContent(layout);
    }

    /// <summary>
    /// Sets each dockable's Content from the live shell view-model, matched by dockable type.
    /// </summary>
    /// <param name="layout">Root of the layout to re-attach.</param>
    public void ReattachContent(IDockable layout)
    {
        foreach (var dockable in FlattenDockables(layout))
        {
            switch (dockable)
            {
                case ControlTreeDockable controlTree:
                    controlTree.Content = _shell.ControlTree;
                    break;
                case RemoteToolsDockable remoteTools:
                    remoteTools.Content = _shell.RemoteTools;
                    break;
                case LiveViewDockable liveView:
                    liveView.Content = _shell.RemoteTools.LiveView;
                    break;
                case LogsDockable logs:
                    logs.Content = _shell.Logs;
                    break;
                case WorkspaceDockable workspace:
                    workspace.Content = _shell.Workspace;
                    break;
            }
        }
    }

    private static IEnumerable<IDockable> FlattenDockables(IDockable dockable)
    {
        yield return dockable;
        if (dockable is IDock dock && dock.VisibleDockables is { } children)
        {
            foreach (var child in children)
            {
                foreach (var nested in FlattenDockables(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
