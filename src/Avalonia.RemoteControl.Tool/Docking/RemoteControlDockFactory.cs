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
    private ToolDock? _liveViewDock;
    private LogsDockable? _logs;
    private ToolDock? _logsDock;

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
        var controlTreeDock = new ToolDock
        {
            Id = "controlTreeDock",
            Alignment = Alignment.Left,
            Proportion = 0.29,
            ActiveDockable = controlTree,
            VisibleDockables = CreateList<IDockable>(controlTree),
        };

        // Center-fill: workspace document well over the logs tool (legacy SouthHeight 220 ≈ 0.35 of the center column).
        var workspaceDock = new DocumentDock
        {
            Id = "workspaceDock",
            IsCollapsable = false,
            Proportion = 0.65,
            ActiveDockable = workspace,
            VisibleDockables = CreateList<IDockable>(workspace),
        };
        var logsDock = new ToolDock
        {
            Id = "logsDock",
            Alignment = Alignment.Bottom,
            Proportion = 0.35,
            ActiveDockable = logs,
            VisibleDockables = CreateList<IDockable>(logs),
        };
        var centerDock = new ProportionalDock
        {
            Id = "centerDock",
            Orientation = Orientation.Vertical,
            Proportion = 0.39,
            VisibleDockables = CreateList<IDockable>(
                workspaceDock,
                new ProportionalDockSplitter(),
                logsDock),
        };

        // East: remote tools over live view (legacy RightDock SouthHeight 360 ≈ 0.58 of the east column).
        var remoteToolsDock = new ToolDock
        {
            Id = "remoteToolsDock",
            Alignment = Alignment.Right,
            Proportion = 0.42,
            ActiveDockable = remoteTools,
            VisibleDockables = CreateList<IDockable>(remoteTools),
        };
        var liveViewDock = new ToolDock
        {
            Id = "liveViewDock",
            Alignment = Alignment.Right,
            Proportion = 0.58,
            ActiveDockable = liveView,
            VisibleDockables = CreateList<IDockable>(liveView),
        };
        var eastDock = new ProportionalDock
        {
            Id = "eastDock",
            Orientation = Orientation.Vertical,
            Proportion = 0.32,
            VisibleDockables = CreateList<IDockable>(
                remoteToolsDock,
                new ProportionalDockSplitter(),
                liveViewDock),
        };

        var main = new ProportionalDock
        {
            Id = "mainLayout",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                controlTreeDock,
                new ProportionalDockSplitter(),
                centerDock,
                new ProportionalDockSplitter(),
                eastDock),
        };

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
        ContextLocator = new Dictionary<string, Func<object?>>
        {
            ["controlTree"] = () => _shell.ControlTree,
            ["remoteTools"] = () => _shell.RemoteTools,
            ["liveView"] = () => _shell.RemoteTools.LiveView,
            ["logs"] = () => _shell.Logs,
            ["workspace"] = () => _shell.Workspace,
        };
        DockableLocator = new Dictionary<string, Func<IDockable?>>();
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new HostWindow(),
        };

        base.InitLayout(layout);
    }
}
