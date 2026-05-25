using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Avalonia.RemoteControl.Client.Security;

/// <summary>
/// Inspects the TLS certificate presented by a remote-control endpoint.
/// </summary>
public static class RemoteControlServerCertificateInspector
{
    /// <summary>
    /// Opens a TLS handshake and returns the server certificate information without persisting trust.
    /// </summary>
    /// <param name="endpoint">HTTPS endpoint to inspect.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Presented certificate information.</returns>
    public static async Task<RemoteControlServerCertificateInfo> InspectAsync(
        Uri endpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        if (!endpoint.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Only HTTPS endpoints expose TLS certificates for inspection.",
                nameof(endpoint));
        }

        X509Certificate2? capturedCertificate = null;
        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(endpoint.Host, endpoint.Port, cancellationToken)
            .ConfigureAwait(false);

        await using var sslStream = new SslStream(
            tcpClient.GetStream(),
            leaveInnerStreamOpen: false,
            (_, certificate, _, _) =>
            {
                capturedCertificate?.Dispose();
                capturedCertificate = certificate switch
                {
                    X509Certificate2 certificate2 => new X509Certificate2(certificate2),
                    null => null,
                    _ => new X509Certificate2(certificate),
                };

                return true;
            });

        try
        {
            await sslStream.AuthenticateAsClientAsync(
                new SslClientAuthenticationOptions
                {
                    TargetHost = endpoint.Host,
                    EnabledSslProtocols = SslProtocols.None,
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                },
                cancellationToken).ConfigureAwait(false);

            if (capturedCertificate is null)
            {
                throw new InvalidOperationException("The server did not present a TLS certificate.");
            }

            return RemoteControlServerCertificateInfo.FromCertificate(capturedCertificate);
        }
        finally
        {
            capturedCertificate?.Dispose();
        }
    }
}
