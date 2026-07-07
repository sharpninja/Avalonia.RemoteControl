using Avalonia.RemoteControl.Tool;
using Avalonia.RemoteControl.Tool.Docking;
using Dock.Model.Core;

namespace Avalonia.RemoteControl.Tests.Docking;

public sealed class RemoteControlDockInteractionTests
{
    [Fact]
    public void ActionsViewModelRaisesInvokeFocusSetPropertyEvents()
    {
        var vm = new ActionsPanelViewModel();
        int click = 0, focus = 0, setProp = 0;
        vm.InvokeClickRequested += (_, _) => click++;
        vm.FocusRequested += (_, _) => focus++;
        vm.SetPropertyRequested += (_, _) => setProp++;

        vm.RequestInvokeClick();
        vm.RequestFocus();
        vm.RequestSetProperty();

        Assert.Equal(1, click);
        Assert.Equal(1, focus);
        Assert.Equal(1, setProp);
    }

    [Fact]
    public void ProjectViewModelRaisesSaveAndRefresh()
    {
        var vm = new ProjectPanelViewModel();
        int save = 0, refresh = 0;
        vm.SaveProjectRequested += (_, _) => save++;
        vm.RefreshRequested += (_, _) => refresh++;

        vm.RequestSaveProject();
        vm.RequestRefresh();

        Assert.Equal(1, save);
        Assert.Equal(1, refresh);
    }

    [Fact]
    public void FactoryShowsAndHidesLiveViewTool()
    {
        var factory = new RemoteControlDockFactory(new RemoteControlToolShellViewModel(Path.GetTempPath()));
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        var dock = FindDock(layout, "liveViewDock");

        Assert.Contains(dock.VisibleDockables!, d => d.Id == "liveView");

        factory.HideLiveViewTool();
        Assert.DoesNotContain(dock.VisibleDockables!, d => d.Id == "liveView");

        factory.ShowLiveViewTool();
        Assert.Contains(dock.VisibleDockables!, d => d.Id == "liveView");

        // Idempotent: showing again does not duplicate.
        factory.ShowLiveViewTool();
        Assert.Single(dock.VisibleDockables!, d => d.Id == "liveView");
    }

    [Fact]
    public void FactoryShowsAndHidesLogsTool()
    {
        var factory = new RemoteControlDockFactory(new RemoteControlToolShellViewModel(Path.GetTempPath()));
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);
        var dock = FindDock(layout, "logsDock");

        Assert.Contains(dock.VisibleDockables!, d => d.Id == "logs");

        factory.HideLogsTool();
        Assert.DoesNotContain(dock.VisibleDockables!, d => d.Id == "logs");

        factory.ShowLogsTool();
        Assert.Contains(dock.VisibleDockables!, d => d.Id == "logs");
    }

    private static IDock FindDock(IDockable root, string id)
        => (IDock)Flatten(root).Single(d => d.Id == id);

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
