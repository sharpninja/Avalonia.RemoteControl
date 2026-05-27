using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Connection settings used by the tool-side MCP server.
/// </summary>
public sealed record RemoteControlMcpOptions(
    Uri Endpoint,
    string Token,
    string TransportProtocol,
    string? CertificatePath,
    string? AcceptedServerCertificateSha256Fingerprint)
{
    /// <summary>
    /// Gets the default endpoint used when no endpoint argument is provided.
    /// </summary>
    public static Uri DefaultEndpoint { get; } = new("http://127.0.0.1:47100/");

    /// <summary>
    /// Creates options from a token value.
    /// </summary>
    /// <param name="endpoint">Remote-control endpoint.</param>
    /// <param name="token">Bearer token.</param>
    /// <param name="transportProtocol">Transport protocol.</param>
    /// <param name="certificatePath">Optional trusted certificate path.</param>
    /// <param name="acceptedServerCertificateSha256Fingerprint">Optional accepted server certificate SHA-256 fingerprint.</param>
    /// <returns>MCP options.</returns>
    public static RemoteControlMcpOptions Create(
        Uri endpoint,
        string token,
        string transportProtocol = RemoteControlProtocol.GrpcTransportProtocol,
        string? certificatePath = null,
        string? acceptedServerCertificateSha256Fingerprint = null)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportProtocol);

        return new RemoteControlMcpOptions(
            endpoint,
            token,
            transportProtocol,
            string.IsNullOrWhiteSpace(certificatePath) ? null : certificatePath,
            string.IsNullOrWhiteSpace(acceptedServerCertificateSha256Fingerprint)
                ? null
                : acceptedServerCertificateSha256Fingerprint);
    }
}
