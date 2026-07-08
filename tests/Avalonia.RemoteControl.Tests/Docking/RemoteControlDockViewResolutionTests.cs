using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.RemoteControl.Tool;
using Avalonia.RemoteControl.Tool.Docking;
using Avalonia.Threading;

namespace Avalonia.RemoteControl.Tests.Docking;

public sealed class RemoteControlDockViewResolutionTests
{
    [AvaloniaFact]
    public void EachDockableResolvesToExpectedControlWithPanelViewModel()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());
        var locator = new RemoteControlDockViewLocator();

        var controlTree = locator.Build(Dockable(RemoteControlDockFactory.ControlTreeId, shell.ControlTree));
        var remoteTools = locator.Build(Dockable(RemoteControlDockFactory.RemoteToolsId, shell.RemoteTools));
        var liveView = locator.Build(Dockable(RemoteControlDockFactory.LiveViewId, shell.RemoteTools.LiveView));
        var logs = locator.Build(Dockable(RemoteControlDockFactory.LogsId, shell.Logs));
        var workspace = locator.Build(Dockable(RemoteControlDockFactory.WorkspaceId, shell.Workspace));

        Assert.IsType<ControlTreePanel>(controlTree);
        Assert.Same(shell.ControlTree, controlTree!.DataContext);

        Assert.IsType<RemoteToolsPanel>(remoteTools);
        Assert.Same(shell.RemoteTools, remoteTools!.DataContext);

        Assert.IsType<LiveViewPanel>(liveView);
        Assert.Same(shell.RemoteTools.LiveView, liveView!.DataContext);

        Assert.IsType<LogPanel>(logs);
        Assert.Same(shell.Logs, logs!.DataContext);

        Assert.IsType<WorkspacePanel>(workspace);
        Assert.Same(shell.Workspace, workspace!.DataContext);
    }

    [AvaloniaFact]
    public void WorkspaceViewBindsTerminalAndPropertiesDataContext()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());
        var locator = new RemoteControlDockViewLocator();

        var view = (WorkspacePanel)locator.Build(Dockable(RemoteControlDockFactory.WorkspaceId, shell.Workspace))!;
        var window = Show(view);
        try
        {
            view.SelectedTabIndex = 0;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(shell.Workspace.Terminal, view.Terminal.DataContext);

            view.SelectedTabIndex = 1;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(shell.Workspace.Properties, view.Properties.DataContext);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void RemoteToolsViewBindsActionsAndProjectDataContext()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());
        var locator = new RemoteControlDockViewLocator();

        var view = (RemoteToolsPanel)locator.Build(Dockable(RemoteControlDockFactory.RemoteToolsId, shell.RemoteTools))!;
        var window = Show(view);
        try
        {
            view.SelectedTabIndex = 0;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(shell.RemoteTools.Actions, view.Actions.DataContext);

            view.SelectedTabIndex = 1;
            Dispatcher.UIThread.RunJobs();
            Assert.Same(shell.RemoteTools.Project, view.Project.DataContext);
        }
        finally
        {
            window.Close();
        }
    }

    private static Dock.Model.Mvvm.Controls.Tool Dockable(string id, object context)
        => new() { Id = id, Context = context };

    // Attach the control to a top level so inherited-DataContext bindings on nested panels activate.
    private static Window Show(Control control)
    {
        var window = new Window
        {
            Content = control,
            Width = 400,
            Height = 300,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }
}
