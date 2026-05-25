namespace Avalonia.RemoteControl.Client.Diagnostics;

/// <summary>
/// Probes a remote-control endpoint for authenticated capabilities.
/// </summary>
public interface IRemoteControlProbe
{
    /// <summary>
    /// Reads endpoint capabilities with bearer authentication.
    /// </summary>
    /// <param name="endpoint">The endpoint URI.</param>
    /// <param name="token">The bearer token.</param>
    /// <param name="transportProtocol">The transport protocol advertised by the endpoint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Endpoint capability data.</returns>
    Task<RemoteControlProbeResult> ProbeAsync(
        Uri endpoint,
        string token,
        string transportProtocol,
        CancellationToken cancellationToken = default);
}
