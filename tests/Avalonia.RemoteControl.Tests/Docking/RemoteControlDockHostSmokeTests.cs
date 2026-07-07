using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.RemoteControl.Tool;
using Avalonia.RemoteControl.Tool.Docking;
using Dock.Avalonia.Controls;

namespace Avalonia.RemoteControl.Tests.Docking;

public sealed class RemoteControlDockHostSmokeTests
{
    [AvaloniaFact]
    public void DockControlHostsFactoryLayoutHeadless()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());
        var factory = new RemoteControlDockFactory(shell);
        var layout = factory.CreateLayout();
        factory.InitLayout(layout);

        var dock = new DockControl
        {
            Factory = factory,
            Layout = layout,
        };

        Assert.Same(layout, dock.Layout);
        Assert.NotNull(layout.ActiveDockable);
    }
}
