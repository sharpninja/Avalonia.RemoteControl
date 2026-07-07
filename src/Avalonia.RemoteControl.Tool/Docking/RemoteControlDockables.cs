using Avalonia.RemoteControl.Client.Logging;
using DockDocument = Dock.Model.Mvvm.Controls.Document;
using DockTool = Dock.Model.Mvvm.Controls.Tool;

namespace Avalonia.RemoteControl.Tool.Docking;

/// <summary>
/// Dockable tool that hosts the control-tree panel view-model.
/// </summary>
public sealed class ControlTreeDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlTreeDockable"/> class.
    /// </summary>
    /// <param name="content">Control-tree panel view-model.</param>
    public ControlTreeDockable(ControlTreePanelViewModel content)
    {
        Content = content;
        Id = "controlTree";
        Title = "Control Tree";
        CanFloat = true;
        CanClose = false;
    }

    /// <summary>
    /// Gets the control-tree panel view-model resolved by the view template.
    /// </summary>
    public ControlTreePanelViewModel Content { get; }
}

/// <summary>
/// Dockable tool that hosts the remote-tools panel view-model.
/// </summary>
public sealed class RemoteToolsDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteToolsDockable"/> class.
    /// </summary>
    /// <param name="content">Remote-tools panel view-model.</param>
    public RemoteToolsDockable(RemoteToolsPanelViewModel content)
    {
        Content = content;
        Id = "remoteTools";
        Title = "Remote Tools";
        CanFloat = true;
        CanClose = false;
    }

    /// <summary>
    /// Gets the remote-tools panel view-model resolved by the view template.
    /// </summary>
    public RemoteToolsPanelViewModel Content { get; }
}

/// <summary>
/// Dockable tool that hosts the live-view panel view-model.
/// </summary>
public sealed class LiveViewDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveViewDockable"/> class.
    /// </summary>
    /// <param name="content">Live-view panel view-model.</param>
    public LiveViewDockable(LiveViewPanelViewModel content)
    {
        Content = content;
        Id = "liveView";
        Title = "Live View";
        CanFloat = true;
        CanClose = true;
    }

    /// <summary>
    /// Gets the live-view panel view-model resolved by the view template.
    /// </summary>
    public LiveViewPanelViewModel Content { get; }
}

/// <summary>
/// Dockable tool that hosts the shared log panel view-model.
/// </summary>
public sealed class LogsDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogsDockable"/> class.
    /// </summary>
    /// <param name="content">Log panel view-model.</param>
    public LogsDockable(RemoteLogViewModel content)
    {
        Content = content;
        Id = "logs";
        Title = "Logs";
        CanFloat = true;
        CanClose = true;
    }

    /// <summary>
    /// Gets the log panel view-model resolved by the view template.
    /// </summary>
    public RemoteLogViewModel Content { get; }
}

/// <summary>
/// Center document that hosts the workspace panel view-model.
/// </summary>
public sealed class WorkspaceDockable : DockDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceDockable"/> class.
    /// </summary>
    /// <param name="content">Workspace panel view-model.</param>
    public WorkspaceDockable(WorkspacePanelViewModel content)
    {
        Content = content;
        Id = "workspace";
        Title = "Workspace";
        CanClose = false;
        CanFloat = false;
    }

    /// <summary>
    /// Gets the workspace panel view-model resolved by the view template.
    /// </summary>
    public WorkspacePanelViewModel Content { get; }
}
