using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Avalonia.RemoteControl.Client.Security;

/// <summary>
/// Represents certificate trust material configured by the desktop client.
/// </summary>
public sealed class RemoteControlServerCertificateTrust
{
    private readonly byte[]? trustedCertificateHash;
    private readonly byte[]? acceptedFingerprintHash;

    private RemoteControlServerCertificateTrust(
        string? trustedCertificatePath,
        string? acceptedSha256Fingerprint)
    {
        TrustedCertificatePath = string.IsNullOrWhiteSpace(trustedCertificatePath)
            ? null
            : trustedCertificatePath;
        AcceptedSha256Fingerprint = NormalizeSha256Fingerprint(acceptedSha256Fingerprint);
        trustedCertificateHash = LoadCertificateHash(TrustedCertificatePath);
        acceptedFingerprintHash = ParseFingerprint(AcceptedSha256Fingerprint);
    }

    /// <summary>
    /// Gets an empty trust configuration.
    /// </summary>
    public static RemoteControlServerCertificateTrust Empty { get; } = new(null, null);

    /// <summary>
    /// Gets the configured certificate file path, if any.
    /// </summary>
    public string? TrustedCertificatePath { get; }

    /// <summary>
    /// Gets the accepted SHA-256 fingerprint, if any.
    /// </summary>
    public string? AcceptedSha256Fingerprint { get; }

    /// <summary>
    /// Gets a value indicating whether explicit certificate trust material is configured.
    /// </summary>
    public bool HasTrustMaterial => trustedCertificateHash is not null || acceptedFingerprintHash is not null;

    /// <summary>
    /// Creates certificate trust from a certificate file and/or an accepted fingerprint.
    /// </summary>
    /// <param name="trustedCertificatePath">Optional certificate file whose SHA-256 fingerprint is trusted.</param>
    /// <param name="acceptedSha256Fingerprint">Optional accepted SHA-256 certificate fingerprint.</param>
    /// <returns>Certificate trust configuration.</returns>
    public static RemoteControlServerCertificateTrust Create(
        string? trustedCertificatePath = null,
        string? acceptedSha256Fingerprint = null)
    {
        if (string.IsNullOrWhiteSpace(trustedCertificatePath)
            && string.IsNullOrWhiteSpace(acceptedSha256Fingerprint))
        {
            return Empty;
        }

        return new RemoteControlServerCertificateTrust(
            trustedCertificatePath,
            acceptedSha256Fingerprint);
    }

    /// <summary>
    /// Gets an uppercase SHA-256 fingerprint for a certificate.
    /// </summary>
    /// <param name="certificate">Certificate to hash.</param>
    /// <returns>Uppercase SHA-256 fingerprint without separators.</returns>
    public static string GetSha256Fingerprint(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);

        return Convert.ToHexString(certificate.GetCertHash(HashAlgorithmName.SHA256));
    }

    /// <summary>
    /// Normalizes a SHA-256 fingerprint to uppercase hexadecimal text without separators.
    /// </summary>
    /// <param name="fingerprint">Fingerprint text.</param>
    /// <returns>Normalized fingerprint, or <see langword="null" /> when no fingerprint is supplied.</returns>
    public static string? NormalizeSha256Fingerprint(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return null;
        }

        var normalized = fingerprint
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        if (normalized.Length != 64 || normalized.Any(static c => !Uri.IsHexDigit(c)))
        {
            throw new ArgumentException(
                "Accepted server certificate fingerprint must be a SHA-256 hexadecimal value.",
                nameof(fingerprint));
        }

        return normalized;
    }

    /// <summary>
    /// Returns whether the presented certificate matches configured trust material.
    /// </summary>
    /// <param name="certificate">Presented server certificate.</param>
    /// <returns><see langword="true" /> when the certificate is explicitly trusted.</returns>
    public bool IsTrusted(X509Certificate2? certificate)
    {
        if (certificate is null)
        {
            return false;
        }

        var hash = certificate.GetCertHash(HashAlgorithmName.SHA256);
        return MatchesHash(hash, trustedCertificateHash)
            || MatchesHash(hash, acceptedFingerprintHash);
    }

    private static bool MatchesHash(byte[] presentedHash, byte[]? trustedHash)
    {
        return trustedHash is not null
            && presentedHash.Length == trustedHash.Length
            && CryptographicOperations.FixedTimeEquals(presentedHash, trustedHash);
    }

    private static byte[]? LoadCertificateHash(string? trustedCertificatePath)
    {
        if (string.IsNullOrWhiteSpace(trustedCertificatePath))
        {
            return null;
        }

        using var certificate = X509CertificateLoader.LoadCertificateFromFile(trustedCertificatePath);
        return certificate.GetCertHash(HashAlgorithmName.SHA256);
    }

    private static byte[]? ParseFingerprint(string? normalizedFingerprint)
    {
        if (string.IsNullOrWhiteSpace(normalizedFingerprint))
        {
            return null;
        }

        return Convert.FromHexString(normalizedFingerprint);
    }
}
