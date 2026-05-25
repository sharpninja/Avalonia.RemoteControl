using System.Net;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Bridge;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Runtime;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlBridgeTcpListenerTests
{
    [Fact]
    public async Task BridgeTcpListenerBindsLoopbackAndAcceptsAuthenticatedCapabilitiesRequest()
    {
        await using var provider = CreateProvider(new TextBlock { Name = "ListenerRoot" });
        var listener = provider.GetRequiredService<RemoteControlBridgeTcpListener>();

        try
        {
            await listener.StartAsync();

            Assert.True(listener.IsRunning);
            Assert.NotNull(listener.BoundEndpoint);
            Assert.Equal(IPAddress.Loopback, listener.BoundEndpoint!.Address);

            using var session = RemoteControlDesktopSession.Create(
                CreateBridgeUri(listener),
                "dev-token",
                transportProtocol: RemoteControlProtocol.AndroidBridgeTransportProtocol);

            var capabilities = await session.GetCapabilitiesAsync();

            Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
            Assert.True(capabilities.SupportsTreeSnapshots);
        }
        finally
        {
            await listener.StopAsync();
        }
    }

    [Fact]
    public async Task BridgeTcpListenerCapturesSnapshotThroughRuntime()
    {
        await using var provider = CreateProvider(new StubRemoteControlRuntime());
        var listener = provider.GetRequiredService<RemoteControlBridgeTcpListener>();

        try
        {
            await listener.StartAsync();

            using var session = RemoteControlDesktopSession.Create(
                CreateBridgeUri(listener),
                "dev-token",
                transportProtocol: RemoteControlProtocol.AndroidBridgeTransportProtocol);

            var snapshot = await session.GetSnapshotAsync();

            Assert.Contains(snapshot.Nodes, node => node.Name == "ListenerSnapshotRoot");
        }
        finally
        {
            await listener.StopAsync();
        }
    }

    [Fact]
    public async Task BridgeEndpointMarkerWritesPackagePrivateJson()
    {
        var marker = RemoteControlBridgeEndpointMarker.Create(47123, "marker-token");
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        try
        {
            var markerPath = await marker.WriteAsync(directory);
            var json = await File.ReadAllTextAsync(markerPath);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal(
                Path.Combine(directory, RemoteControlBridgeEndpointMarker.FileName),
                markerPath);
            Assert.Equal("1", root.GetProperty("schemaVersion").GetString());
            Assert.Equal(47123, root.GetProperty("devicePort").GetInt32());
            Assert.Equal("marker-token", root.GetProperty("token").GetString());
            Assert.Equal(
                RemoteControlProtocol.AndroidBridgeTransportProtocol,
                root.GetProperty("bridgeProtocol").GetString());
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BridgeTcpListenerCreatesMarkerFromBoundEndpoint()
    {
        await using var provider = CreateProvider(new TextBlock());
        var listener = provider.GetRequiredService<RemoteControlBridgeTcpListener>();

        try
        {
            await listener.StartAsync();

            var marker = listener.CreateEndpointMarker();

            Assert.Equal(listener.BoundEndpoint!.Port, marker.DevicePort);
            Assert.Equal("dev-token", marker.Token);
            Assert.Equal(RemoteControlProtocol.AndroidBridgeTransportProtocol, marker.BridgeProtocol);
        }
        finally
        {
            await listener.StopAsync();
        }
    }

    private static Uri CreateBridgeUri(RemoteControlBridgeTcpListener listener)
    {
        return new Uri($"http://127.0.0.1:{listener.BoundEndpoint!.Port}");
    }

    private static ServiceProvider CreateProvider(Control root)
    {
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControlRuntime(options =>
        {
            options.AuthenticationToken = "dev-token";
            options.Port = 0;
            options.IsAdbTunnel = true;
        });
        services.AddSingleton<IRemoteControlDispatcher, InlineRemoteControlDispatcher>();
        services.AddSingleton<IRemoteControlRootProvider>(new StaticRemoteControlRootProvider(root));

        return services.BuildServiceProvider();
    }

    private static ServiceProvider CreateProvider(IRemoteControlRuntime runtime)
    {
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControlRuntime(options =>
        {
            options.AuthenticationToken = "dev-token";
            options.Port = 0;
            options.IsAdbTunnel = true;
        });
        services.AddSingleton(runtime);

        return services.BuildServiceProvider();
    }

    private sealed class StubRemoteControlRuntime : IRemoteControlRuntime
    {
        public RemoteControlCapabilities GetCapabilities()
        {
            return new RemoteControlCapabilities();
        }

        public ValueTask<RemoteControlTreeSnapshot> GetSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return ValueTask.FromResult(new RemoteControlTreeSnapshot(
                1,
                [
                    new RemoteControlNodeSnapshot
                    {
                        Id = "node-1",
                        TypeName = nameof(TextBlock),
                        Name = "ListenerSnapshotRoot",
                        Bounds = new RemoteControlRect(0, 0, 100, 32),
                        IsVisible = true,
                        IsEnabled = true,
                    },
                ]));
        }

        public async IAsyncEnumerable<RemoteControlTreeSnapshot> WatchSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return await GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        public ValueTask<RemoteControlCommandResult> InvokeClickAsync(
            string nodeId,
            string clientIdentity,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RemoteControlCommandResult(false, "Not implemented."));
        }

        public ValueTask<RemoteControlCommandResult> InvokeFocusAsync(
            string nodeId,
            string clientIdentity,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RemoteControlCommandResult(false, "Not implemented."));
        }

        public ValueTask<RemoteControlCommandResult> SetPropertyAsync(
            string nodeId,
            string propertyName,
            string value,
            string clientIdentity,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(new RemoteControlCommandResult(false, "Not implemented."));
        }

        public async IAsyncEnumerable<RemoteControlLogEntry> WatchLogEntriesAsync(
            LogLevel minimumLevel,
            string? categoryPrefix,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask.ConfigureAwait(false);
            yield break;
        }
    }
}
