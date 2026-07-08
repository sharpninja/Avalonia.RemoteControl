using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Dock.Model.Core;

namespace Avalonia.RemoteControl.Tool.Docking;

/// <summary>
/// Resolves each remote-control dockable (a Dock built-in tool/document identified by id) to its existing
/// panel <see cref="UserControl"/>, binding the control's <see cref="StyledElement.DataContext"/> to the
/// dockable's panel view-model (its <see cref="IDockable.Context"/>). Registered in
/// <c>Application.DataTemplates</c> so <c>DockControl</c> renders each pane.
/// </summary>
public sealed class RemoteControlDockViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is not IDockable dockable)
        {
            return null;
        }

        Control? view = dockable.Id switch
        {
            RemoteControlDockFactory.ControlTreeId => new ControlTreePanel(),
            RemoteControlDockFactory.RemoteToolsId => new RemoteToolsPanel(),
            RemoteControlDockFactory.LiveViewId => new LiveViewPanel(),
            RemoteControlDockFactory.LogsId => new LogPanel(),
            RemoteControlDockFactory.WorkspaceId => new WorkspacePanel(),
            _ => null,
        };

        if (view is not null)
        {
            view.DataContext = dockable.Context;
        }

        return view;
    }

    /// <inheritdoc />
    public bool Match(object? data)
    {
        return data is IDockable dockable && dockable.Id is
            RemoteControlDockFactory.ControlTreeId
            or RemoteControlDockFactory.RemoteToolsId
            or RemoteControlDockFactory.LiveViewId
            or RemoteControlDockFactory.LogsId
            or RemoteControlDockFactory.WorkspaceId;
    }
}
