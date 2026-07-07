using Avalonia.RemoteControl.Tool;
using Avalonia.RemoteControl.Tool.Docking;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace Avalonia.RemoteControl.Tests.Docking;

public sealed class RemoteControlDockFactoryTests
{
    private static RemoteControlDockFactory CreateFactory()
        => new(new RemoteControlToolShellViewModel(Path.GetTempPath()));

    [Fact]
    public void CreateLayoutReturnsInitializedRootDock()
    {
        var factory = CreateFactory();

        var layout = factory.CreateLayout();

        Assert.NotNull(layout);

        factory.InitLayout(layout);

        Assert.NotNull(layout.ActiveDockable);
        Assert.NotNull(layout.DefaultDockable);
    }

    [Fact]
    public void LayoutPlacesControlTreeWestRemoteToolsAndLiveViewEastLogsSouthWorkspaceCenter()
    {
        var layout = CreateFactory().CreateLayout();

        var main = Assert.IsAssignableFrom<IProportionalDock>(layout.VisibleDockables!.Single());
        Assert.Equal(Orientation.Horizontal, main.Orientation);

        var controlTreeDock = Find<IToolDock>(layout, "controlTreeDock");
        Assert.Contains(controlTreeDock.VisibleDockables!, d => d.Id == "controlTree");

        var centerDock = Find<IProportionalDock>(layout, "centerDock");
        Assert.Equal(Orientation.Vertical, centerDock.Orientation);
        var workspaceDock = Find<IDocumentDock>(layout, "workspaceDock");
        Assert.Contains(workspaceDock.VisibleDockables!, d => d.Id == "workspace");
        var logsDock = Find<IToolDock>(layout, "logsDock");
        Assert.Contains(logsDock.VisibleDockables!, d => d.Id == "logs");

        var eastDock = Find<IProportionalDock>(layout, "eastDock");
        Assert.Equal(Orientation.Vertical, eastDock.Orientation);
        var remoteToolsDock = Find<IToolDock>(layout, "remoteToolsDock");
        Assert.Contains(remoteToolsDock.VisibleDockables!, d => d.Id == "remoteTools");
        var liveViewDock = Find<IToolDock>(layout, "liveViewDock");
        Assert.Contains(liveViewDock.VisibleDockables!, d => d.Id == "liveView");

        // Horizontal order: control tree (west) -> center -> east.
        var mainKids = main.VisibleDockables!.ToList();
        Assert.True(mainKids.IndexOf(controlTreeDock) < mainKids.IndexOf(centerDock));
        Assert.True(mainKids.IndexOf(centerDock) < mainKids.IndexOf(eastDock));

        // Logs sits below the workspace document in the center column.
        var centerKids = centerDock.VisibleDockables!.ToList();
        Assert.True(centerKids.IndexOf(workspaceDock) < centerKids.IndexOf(logsDock));

        // Live view sits below remote tools in the east column.
        var eastKids = eastDock.VisibleDockables!.ToList();
        Assert.True(eastKids.IndexOf(remoteToolsDock) < eastKids.IndexOf(liveViewDock));
    }

    [Fact]
    public void LayoutUsesDefaultProportionsMatchingLegacySizes()
    {
        var layout = CreateFactory().CreateLayout();

        // Legacy defaults: West 340, East 390 of 1180 wide; center column ~0.39.
        Assert.InRange(Find<IToolDock>(layout, "controlTreeDock").Proportion, 0.24, 0.34);
        Assert.InRange(Find<IProportionalDock>(layout, "centerDock").Proportion, 0.34, 0.44);
        Assert.InRange(Find<IProportionalDock>(layout, "eastDock").Proportion, 0.27, 0.37);
        // Legacy SouthHeight 220 (logs) and RightDock south 360 (live view).
        Assert.InRange(Find<IToolDock>(layout, "logsDock").Proportion, 0.30, 0.40);
        Assert.InRange(Find<IToolDock>(layout, "liveViewDock").Proportion, 0.53, 0.63);
    }

    [Fact]
    public void DockableCapabilitiesMatchLegacyBehavior()
    {
        var layout = CreateFactory().CreateLayout();

        Assert.False(Find<WorkspaceDockable>(layout, "workspace").CanClose);

        Assert.True(Find<LiveViewDockable>(layout, "liveView").CanClose);
        Assert.True(Find<LiveViewDockable>(layout, "liveView").CanFloat);
        Assert.True(Find<LogsDockable>(layout, "logs").CanClose);
        Assert.True(Find<LogsDockable>(layout, "logs").CanFloat);

        Assert.True(Find<ControlTreeDockable>(layout, "controlTree").CanFloat);
        Assert.True(Find<RemoteToolsDockable>(layout, "remoteTools").CanFloat);
    }

    private static T Find<T>(IDockable root, string id)
        where T : class, IDockable
        => (T)Flatten(root).Single(d => d.Id == id);

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
