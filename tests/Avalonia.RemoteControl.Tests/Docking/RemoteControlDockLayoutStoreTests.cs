using Avalonia.RemoteControl.Tool;
using Avalonia.RemoteControl.Tool.Docking;
using Dock.Model.Core;

namespace Avalonia.RemoteControl.Tests.Docking;

public sealed class RemoteControlDockLayoutStoreTests
{
    [Fact]
    public void SaveThenLoadRoundTripsDockTree()
    {
        var root = Path.Combine(Path.GetTempPath(), "arc-layout-" + Guid.NewGuid().ToString("N"));
        try
        {
            var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());
            var factory = new RemoteControlDockFactory(shell);
            var layout = factory.CreateLayout();
            factory.InitLayout(layout);
            var store = new RemoteControlDockLayoutStore(root);

            store.Save(layout, "default");
            Assert.True(File.Exists(store.GetLayoutPath("default")));

            var loaded = store.Load("default", factory);

            Assert.NotNull(loaded);
            var ids = Flatten(loaded!).Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
            Assert.Contains("controlTree", ids);
            Assert.Contains("workspace", ids);
            Assert.Contains("logs", ids);
            Assert.Contains("remoteTools", ids);
            Assert.Contains("liveView", ids);

            // Panel view-models are re-attached from the live shell (not stale deserialized copies).
            var controlTree = Flatten(loaded!).Single(d => d.Id == "controlTree");
            Assert.Same(shell.ControlTree, controlTree.Context);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void LoadReturnsNullWhenNoLayoutFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "arc-layout-" + Guid.NewGuid().ToString("N"));
        var factory = new RemoteControlDockFactory(new RemoteControlToolShellViewModel(Path.GetTempPath()));
        var store = new RemoteControlDockLayoutStore(root);

        Assert.Null(store.Load("default", factory));
    }

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
