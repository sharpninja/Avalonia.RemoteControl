namespace Avalonia.RemoteControl.Client.Diagnostics;

/// <summary>
/// Probes a remote-control endpoint through the advertised remote-control transport.
/// </summary>
public sealed class GrpcRemoteControlProbe : IRemoteControlProbe
{
    /// <inheritdoc />
    public async Task<RemoteControlProbeResult> ProbeAsync(
        Uri endpoint,
        string token,
        string transportProtocol,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportProtocol);

        using var session = RemoteControlDesktopSession.Create(
            endpoint,
            token,
            transportProtocol: transportProtocol);
        var capabilities = await session.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);

        return new RemoteControlProbeResult(
            capabilities.ProtocolVersion,
            capabilities.AuthenticatedClientIdentity,
            capabilities.SupportsTreeSnapshots,
            capabilities.SupportsTreeStreaming,
            capabilities.SupportsClickInvocation,
            capabilities.SupportsPropertyMutation,
            capabilities.SupportsLogStreaming,
            capabilities.SupportsFrameStreaming,
            capabilities.SupportsRemoteInput);
    }
}
