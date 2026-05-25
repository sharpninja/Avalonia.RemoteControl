using System.Net;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlHostedServerTests
{
    [Fact]
    public async Task HostedGrpcServerRequiresBearerTokenAndServesAuthenticatedRequests()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

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

            Assert.NotNull(host.BoundAddress);

            using var channel = GrpcChannel.ForAddress(host.BoundAddress!);
            var client = new Protocol.V1.RemoteControl.RemoteControlClient(channel);

            var unauthenticated = await Assert.ThrowsAsync<RpcException>(async () =>
                await client.GetCapabilitiesAsync(new GetCapabilitiesRequest()));

            Assert.Equal(StatusCode.Unauthenticated, unauthenticated.StatusCode);

            var authenticated = await client.GetCapabilitiesAsync(
                new GetCapabilitiesRequest(),
                new global::Grpc.Core.Metadata { { "authorization", "Bearer dev-token" } });

            Assert.Equal(RemoteControlProtocol.DisplayVersion, authenticated.ProtocolVersion);
            Assert.True(authenticated.SupportsTreeSnapshots);
            Assert.True(authenticated.SupportsLogStreaming);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
