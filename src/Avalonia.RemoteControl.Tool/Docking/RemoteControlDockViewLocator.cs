using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Avalonia.RemoteControl.Tool.Docking;

/// <summary>
/// Resolves each remote-control dockable view-model to its existing panel <see cref="UserControl"/>,
/// binding the control's <see cref="StyledElement.DataContext"/> to the dockable's panel view-model.
/// Registered in <c>Application.DataTemplates</c> so <c>DockControl</c> renders each pane.
/// </summary>
public sealed class RemoteControlDockViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? param)
    {
        return param switch
        {
            ControlTreeDockable dockable => new ControlTreePanel { DataContext = dockable.Content },
            RemoteToolsDockable dockable => new RemoteToolsPanel { DataContext = dockable.Content },
            LiveViewDockable dockable => new LiveViewPanel { DataContext = dockable.Content },
            LogsDockable dockable => new LogPanel { DataContext = dockable.Content },
            WorkspaceDockable dockable => new WorkspacePanel { DataContext = dockable.Content },
            _ => null,
        };
    }

    /// <inheritdoc />
    public bool Match(object? data)
    {
        return data is ControlTreeDockable
            or RemoteToolsDockable
            or LiveViewDockable
            or LogsDockable
            or WorkspaceDockable;
    }
}
