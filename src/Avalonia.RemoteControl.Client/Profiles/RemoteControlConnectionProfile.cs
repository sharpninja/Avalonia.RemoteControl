using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Client.Profiles;

/// <summary>
/// Represents a saved remote-control connection profile.
/// </summary>
public sealed record RemoteControlConnectionProfile
{
    /// <summary>
    /// Gets or sets the stable app identifier for project-scoped settings.
    /// </summary>
    public string AppId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the human-readable profile or app display name.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint URI text.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the bearer token.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional client-trusted certificate path for TLS profiles.
    /// </summary>
    public string CertificatePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the accepted server certificate SHA-256 fingerprint for TLS profiles.
    /// </summary>
    public string AcceptedServerCertificateSha256Fingerprint { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the transport protocol used by the endpoint.
    /// </summary>
    public string TransportProtocol { get; init; } = RemoteControlProtocol.GrpcTransportProtocol;

    /// <summary>
    /// Gets or sets the client connection mode that produced this profile.
    /// </summary>
    public string ConnectionMode { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Android package name for ADB-backed profiles.
    /// </summary>
    public string AndroidPackageName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Android device serial for ADB-backed profiles.
    /// </summary>
    public string AndroidSerial { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the host-side ADB forwarded port.
    /// </summary>
    public int? AdbHostPort { get; init; }

    /// <summary>
    /// Gets or sets the device-side remote-control port when it is known.
    /// </summary>
    public int? AdbDevicePort { get; init; }

    /// <summary>
    /// Gets or sets the profile update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
