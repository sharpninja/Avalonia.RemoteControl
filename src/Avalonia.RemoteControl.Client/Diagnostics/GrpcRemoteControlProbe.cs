using Avalonia.RemoteControl.Protocol.V1;
using Grpc.Net.Client;

namespace Avalonia.RemoteControl.Client.Diagnostics;

/// <summary>
/// Probes a remote-control endpoint through the gRPC protocol.
/// </summary>
public sealed class GrpcRemoteControlProbe : IRemoteControlProbe
{
    /// <inheritdoc />
    public async Task<RemoteControlProbeResult> ProbeAsync(
        Uri endpoint,
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        using var channel = GrpcChannel.ForAddress(endpoint);
        var client = new Protocol.V1.RemoteControl.RemoteControlClient(channel);
        var capabilities = await client.GetCapabilitiesAsync(
            new GetCapabilitiesRequest(),
            new global::Grpc.Core.Metadata { { "authorization", $"Bearer {token}" } },
            cancellationToken: cancellationToken);

        return new RemoteControlProbeResult(
            capabilities.ProtocolVersion,
            capabilities.SupportsTreeSnapshots,
            capabilities.SupportsTreeStreaming,
            capabilities.SupportsClickInvocation,
            capabilities.SupportsPropertyMutation,
            capabilities.SupportsLogStreaming);
    }
}
