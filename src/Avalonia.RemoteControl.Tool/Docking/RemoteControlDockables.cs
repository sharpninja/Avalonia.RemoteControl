using System.Text.Json.Serialization;
using Avalonia.RemoteControl.Client.Logging;
using DockDocument = Dock.Model.Mvvm.Controls.Document;
using DockTool = Dock.Model.Mvvm.Controls.Tool;

namespace Avalonia.RemoteControl.Tool.Docking;

// Each dockable's panel view-model is carried in a [JsonIgnore] Content property (never serialized),
// which the factory re-attaches from the live shell in InitLayout after a layout is loaded. The view
// locator binds Content. Dock's Context is intentionally left unset so the serializer never walks the
// (cyclic) view-model graph.

/// <summary>
/// Dockable tool that hosts the control-tree panel view-model.
/// </summary>
public sealed class ControlTreeDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlTreeDockable"/> class.
    /// </summary>
    public ControlTreeDockable()
    {
        Id = "controlTree";
        Title = "Control Tree";
        CanFloat = true;
        CanClose = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ControlTreeDockable"/> class with content.
    /// </summary>
    /// <param name="content">Control-tree panel view-model.</param>
    public ControlTreeDockable(ControlTreePanelViewModel content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Gets the control-tree panel view-model resolved by the view template.
    /// </summary>
    [JsonIgnore]
    public ControlTreePanelViewModel? Content { get; set; }
}

/// <summary>
/// Dockable tool that hosts the remote-tools panel view-model.
/// </summary>
public sealed class RemoteToolsDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteToolsDockable"/> class.
    /// </summary>
    public RemoteToolsDockable()
    {
        Id = "remoteTools";
        Title = "Remote Tools";
        CanFloat = true;
        CanClose = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteToolsDockable"/> class with content.
    /// </summary>
    /// <param name="content">Remote-tools panel view-model.</param>
    public RemoteToolsDockable(RemoteToolsPanelViewModel content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Gets the remote-tools panel view-model resolved by the view template.
    /// </summary>
    [JsonIgnore]
    public RemoteToolsPanelViewModel? Content { get; set; }
}

/// <summary>
/// Dockable tool that hosts the live-view panel view-model.
/// </summary>
public sealed class LiveViewDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveViewDockable"/> class.
    /// </summary>
    public LiveViewDockable()
    {
        Id = "liveView";
        Title = "Live View";
        CanFloat = true;
        CanClose = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LiveViewDockable"/> class with content.
    /// </summary>
    /// <param name="content">Live-view panel view-model.</param>
    public LiveViewDockable(LiveViewPanelViewModel content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Gets the live-view panel view-model resolved by the view template.
    /// </summary>
    [JsonIgnore]
    public LiveViewPanelViewModel? Content { get; set; }
}

/// <summary>
/// Dockable tool that hosts the shared log panel view-model.
/// </summary>
public sealed class LogsDockable : DockTool
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LogsDockable"/> class.
    /// </summary>
    public LogsDockable()
    {
        Id = "logs";
        Title = "Logs";
        CanFloat = true;
        CanClose = true;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogsDockable"/> class with content.
    /// </summary>
    /// <param name="content">Log panel view-model.</param>
    public LogsDockable(RemoteLogViewModel content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Gets the log panel view-model resolved by the view template.
    /// </summary>
    [JsonIgnore]
    public RemoteLogViewModel? Content { get; set; }
}

/// <summary>
/// Center document that hosts the workspace panel view-model.
/// </summary>
public sealed class WorkspaceDockable : DockDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceDockable"/> class.
    /// </summary>
    public WorkspaceDockable()
    {
        Id = "workspace";
        Title = "Workspace";
        CanClose = false;
        CanFloat = false;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceDockable"/> class with content.
    /// </summary>
    /// <param name="content">Workspace panel view-model.</param>
    public WorkspaceDockable(WorkspacePanelViewModel content)
        : this()
    {
        Content = content;
    }

    /// <summary>
    /// Gets the workspace panel view-model resolved by the view template.
    /// </summary>
    [JsonIgnore]
    public WorkspacePanelViewModel? Content { get; set; }
}
