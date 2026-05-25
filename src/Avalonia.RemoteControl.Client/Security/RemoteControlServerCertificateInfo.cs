using System.Security.Cryptography.X509Certificates;

namespace Avalonia.RemoteControl.Client.Security;

/// <summary>
/// Describes a TLS server certificate presented by a remote-control endpoint.
/// </summary>
public sealed record RemoteControlServerCertificateInfo
{
    /// <summary>
    /// Gets the certificate subject.
    /// </summary>
    public string Subject { get; init; } = string.Empty;

    /// <summary>
    /// Gets the certificate issuer.
    /// </summary>
    public string Issuer { get; init; } = string.Empty;

    /// <summary>
    /// Gets the UTC time before which the certificate is not valid.
    /// </summary>
    public DateTimeOffset NotBefore { get; init; }

    /// <summary>
    /// Gets the UTC time after which the certificate is not valid.
    /// </summary>
    public DateTimeOffset NotAfter { get; init; }

    /// <summary>
    /// Gets the uppercase SHA-256 certificate fingerprint without separators.
    /// </summary>
    public string Sha256Fingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Creates certificate information from an X.509 certificate.
    /// </summary>
    /// <param name="certificate">The certificate to describe.</param>
    /// <returns>Certificate information suitable for display and trust persistence.</returns>
    public static RemoteControlServerCertificateInfo FromCertificate(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        return new RemoteControlServerCertificateInfo
        {
            Subject = certificate.Subject,
            Issuer = certificate.Issuer,
            NotBefore = new DateTimeOffset(certificate.NotBefore.ToUniversalTime(), TimeSpan.Zero),
            NotAfter = new DateTimeOffset(certificate.NotAfter.ToUniversalTime(), TimeSpan.Zero),
            Sha256Fingerprint = RemoteControlServerCertificateTrust.GetSha256Fingerprint(certificate),
        };
    }
}
