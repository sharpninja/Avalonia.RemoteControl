using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using DockDocument = Dock.Model.Mvvm.Controls.Document;
using DockTool = Dock.Model.Mvvm.Controls.Tool;

namespace Avalonia.RemoteControl.Tool.Docking;

/// <summary>
/// Builds and initializes the Dock.Avalonia layout for the remote-control tool shell.
/// Dockables are Dock's built-in <see cref="DockTool"/>/<see cref="DockDocument"/> identified by id, each
/// carrying its panel view-model in <see cref="Dock.Model.Core.IDockable.Context"/> (re-attached from the
/// live shell by <c>ContextLocator</c> on load). Using the built-in types keeps them inside Dock's
/// serializer type-context, so layout round-trips cleanly.
/// </summary>
public sealed class RemoteControlDockFactory : Factory
{
    /// <summary>Dockable id for the control-tree tool.</summary>
    public const string ControlTreeId = "controlTree";

    /// <summary>Dockable id for the remote-tools tool.</summary>
    public const string RemoteToolsId = "remoteTools";

    /// <summary>Dockable id for the live-view tool.</summary>
    public const string LiveViewId = "liveView";

    /// <summary>Dockable id for the logs tool.</summary>
    public const string LogsId = "logs";

    /// <summary>Dockable id for the workspace document.</summary>
    public const string WorkspaceId = "workspace";

    private readonly RemoteControlToolShellViewModel _shell;

    private IDockable? _liveView;
    private IToolDock? _liveViewDock;
    private IDockable? _logs;
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
        var controlTree = MakeTool(ControlTreeId, "Control Tree", canFloat: true, canClose: false, _shell.ControlTree);
        var remoteTools = MakeTool(RemoteToolsId, "Remote Tools", canFloat: true, canClose: false, _shell.RemoteTools);
        var liveView = MakeTool(LiveViewId, "Live View", canFloat: true, canClose: true, _shell.RemoteTools.LiveView);
        var logs = MakeTool(LogsId, "Logs", canFloat: true, canClose: true, _shell.Logs);

        var workspace = CreateDocument();
        workspace.Id = WorkspaceId;
        workspace.Title = "Workspace";
        workspace.CanClose = false;
        workspace.CanFloat = false;
        workspace.Context = _shell.Workspace;

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

    /// <inheritdoc />
    public override void InitLayout(IDockable layout)
    {
        // ContextLocator re-attaches each dockable's panel view-model from the live shell by id — used for
        // the fresh layout and, critically, to reconnect a deserialized (loaded) layout to the running shell.
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            [ControlTreeId] = () => _shell.ControlTree,
            [RemoteToolsId] = () => _shell.RemoteTools,
            [LiveViewId] = () => _shell.RemoteTools.LiveView,
            [LogsId] = () => _shell.Logs,
            [WorkspaceId] = () => _shell.Workspace,
        };
        DockableLocator = new Dictionary<string, Func<IDockable?>>();
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow(),
        };

        base.InitLayout(layout);

        // Re-bind the runtime handles for show/hide after a load reconstructs the tree.
        _liveViewDock = FindDock(layout, "liveViewDock");
        _logsDock = FindDock(layout, "logsDock");
        _liveView = FindDockable(layout, LiveViewId);
        _logs = FindDockable(layout, LogsId);
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

    private ITool MakeTool(string id, string title, bool canFloat, bool canClose, object context)
    {
        var tool = CreateTool();
        tool.Id = id;
        tool.Title = title;
        tool.CanFloat = canFloat;
        tool.CanClose = canClose;
        tool.Context = context;
        return tool;
    }

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

    private static IToolDock? FindDock(IDockable layout, string id)
        => Flatten(layout).OfType<IToolDock>().FirstOrDefault(d => d.Id == id);

    private static IDockable? FindDockable(IDockable layout, string id)
        => Flatten(layout).FirstOrDefault(d => d.Id == id);

    private static IEnumerable<IDockable> Flatten(IDockable dockable)
    {
        yield return dockable;
        if (dockable is IDock dock && dock.VisibleDockables is { } children)
        {
            foreach (var child in children)
            {
                foreach (var nested in Flatten(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
