using System.Net;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlDesktopSessionTests
{
    [Fact]
    public async Task DesktopSessionReadsCapabilitiesFromHostedServer()
    {
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl(options =>
        {
            options.IsEnabled = true;
            options.Host = IPAddress.Loopback;
            options.Port = 0;
            options.AuthenticationToken = "dev-token";
        });

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<AvaloniaRemoteControlServerHost>();

        try
        {
            await host.StartAsync();

            using var session = RemoteControlDesktopSession.Create(host.BoundAddress!, "dev-token");
            var capabilities = await session.GetCapabilitiesAsync();

            Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
            Assert.True(capabilities.SupportsTreeSnapshots);
            Assert.True(capabilities.SupportsLogStreaming);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
