using Avalonia.RemoteControl.Tool.Docking;

namespace Avalonia.RemoteControl.Tests.Docking;

public sealed class RemoteControlLegacyDockRemovedTests
{
    [Fact]
    public void NoLegacyDockTypesRemain()
    {
        var assembly = typeof(RemoteControlDockFactory).Assembly;

        Assert.Null(assembly.GetType("Avalonia.RemoteControl.Tool.DockLayout"));
        Assert.Null(assembly.GetType("Avalonia.RemoteControl.Tool.DockPaneChrome"));
        Assert.Null(assembly.GetType("Avalonia.RemoteControl.Tool.FloatingDockPaneWindow"));
        Assert.Null(assembly.GetType("Avalonia.RemoteControl.Tool.DockRegion"));
        Assert.Null(assembly.GetType("Avalonia.RemoteControl.Tool.IDockAutoHideHost"));
    }
}
