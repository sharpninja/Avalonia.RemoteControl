using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlHostingTests
{
    [Fact]
    public async Task ServiceProviderStartupHelperDoesNotBindWhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl();

        await using var provider = services.BuildServiceProvider();

        await provider.StartAvaloniaRemoteControlAsync();
        var host = provider.GetRequiredService<AvaloniaRemoteControlServerHost>();
        await provider.StopAvaloniaRemoteControlAsync();

        Assert.Null(host.BoundAddress);
    }
}
