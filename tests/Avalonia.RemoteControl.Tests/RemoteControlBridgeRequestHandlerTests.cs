using Avalonia.Controls;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Bridge;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlBridgeRequestHandlerTests
{
    [Fact]
    public async Task BridgeHandlerRejectsMissingBearerBeforeDispatch()
    {
        await using var provider = CreateProvider(new TextBlock(), options =>
        {
            options.AuthenticationToken = "dev-token";
        });

        var response = await provider.GetRequiredService<RemoteControlBridgeRequestHandler>()
            .HandleAsync(new BridgeRequest
            {
                ProtocolVersion = RemoteControlProtocol.DisplayVersion,
                RequestId = "req-auth-001",
                Method = BridgeMethod.GetCapabilities,
                Payload = new GetCapabilitiesRequest().ToByteString(),
            });

        Assert.Equal(BridgeStatus.Unauthenticated, response.Status);
        Assert.Equal("req-auth-001", response.RequestId);
        Assert.True(response.EndOfStream);
        Assert.Empty(response.Payload);
        Assert.DoesNotContain("dev-token", response.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BridgeHandlerDispatchesGetCapabilities()
    {
        await using var provider = CreateProvider(new TextBlock(), options =>
        {
            options.AuthenticationToken = "dev-token";
        });

        var response = await provider.GetRequiredService<RemoteControlBridgeRequestHandler>()
            .HandleAsync(CreateRequest(
                "req-cap-001",
                BridgeMethod.GetCapabilities,
                new GetCapabilitiesRequest().ToByteString()));

        var capabilities = GetCapabilitiesResponse.Parser.ParseFrom(response.Payload);

        Assert.Equal(BridgeStatus.Ok, response.Status);
        Assert.True(response.EndOfStream);
        Assert.Equal(RemoteControlProtocol.DisplayVersion, response.ProtocolVersion);
        Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
        Assert.True(capabilities.SupportsTreeSnapshots);
    }

    [Fact]
    public async Task BridgeHandlerDispatchesSnapshotCapture()
    {
        var root = new TextBlock { Name = "BridgeRoot", Text = "Hello bridge" };
        await using var provider = CreateProvider(root, options =>
        {
            options.AuthenticationToken = "dev-token";
        });

        var response = await provider.GetRequiredService<RemoteControlBridgeRequestHandler>()
            .HandleAsync(CreateRequest(
                "req-snapshot-001",
                BridgeMethod.GetSnapshot,
                new GetSnapshotRequest().ToByteString()));

        var snapshot = TreeSnapshot.Parser.ParseFrom(response.Payload);

        Assert.Equal(BridgeStatus.Ok, response.Status);
        Assert.Contains(snapshot.Nodes, node => node.Name == "BridgeRoot");
    }

    [Fact]
    public async Task BridgeHandlerDispatchesSetPropertyThroughMutationPolicy()
    {
        var root = new TextBlock { Text = "Before" };
        await using var provider = CreateProvider(root, options =>
        {
            options.AuthenticationToken = "dev-token";
            options.AllowedMutableProperties.Add(nameof(TextBlock.Text));
        });

        var snapshot = await provider.GetRequiredService<IControlTreeSnapshotProvider>()
            .CaptureSnapshotAsync(root);

        var response = await provider.GetRequiredService<RemoteControlBridgeRequestHandler>()
            .HandleAsync(CreateRequest(
                "req-set-001",
                BridgeMethod.SetProperty,
                new SetPropertyRequest
                {
                    NodeId = snapshot.Nodes[0].Id,
                    PropertyName = nameof(TextBlock.Text),
                    Value = "After",
                }.ToByteString()));

        var result = CommandResult.Parser.ParseFrom(response.Payload);

        Assert.Equal(BridgeStatus.Ok, response.Status);
        Assert.True(result.Succeeded);
        Assert.Equal("After", root.Text);
    }

    [Fact]
    public async Task BridgeHandlerReturnsStreamFailureWhenFramesDisabledByPolicy()
    {
        await using var provider = CreateProvider(new TextBlock(), options =>
        {
            options.AuthenticationToken = "dev-token";
        });

        await using var enumerator = provider.GetRequiredService<RemoteControlBridgeRequestHandler>()
            .HandleResponsesAsync(CreateRequest(
                "req-frame-policy-001",
                BridgeMethod.WatchFrames,
                new WatchFramesRequest().ToByteString()))
            .GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(BridgeStatus.Error, enumerator.Current.Status);
        Assert.True(enumerator.Current.EndOfStream);
        Assert.Contains("disabled", enumerator.Current.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static BridgeRequest CreateRequest(
        string requestId,
        BridgeMethod method,
        ByteString payload)
    {
        return new BridgeRequest
        {
            ProtocolVersion = RemoteControlProtocol.DisplayVersion,
            RequestId = requestId,
            Method = method,
            Authorization = RemoteControlBridgeProtocol.CreateBearerAuthorization("dev-token"),
            Payload = payload,
        };
    }

    private static ServiceProvider CreateProvider(
        Control root,
        Action<AvaloniaRemoteControlOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl(configure);
        services.AddSingleton<IRemoteControlDispatcher, InlineRemoteControlDispatcher>();
        services.AddSingleton<IRemoteControlRootProvider>(new StaticRemoteControlRootProvider(root));

        return services.BuildServiceProvider();
    }
}
