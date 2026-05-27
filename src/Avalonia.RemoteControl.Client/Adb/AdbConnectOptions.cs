using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Options for creating an ADB-backed remote-control connection.
/// </summary>
public sealed record AdbConnectOptions
{
    /// <summary>
    /// Gets or sets the adb device serial.
    /// </summary>
    public string Serial { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the Android package name used for endpoint discovery.
    /// </summary>
    public string? PackageName { get; init; }

    /// <summary>
    /// Gets or sets the Android-side remote-control port.
    /// </summary>
    public int? DevicePort { get; init; }

    /// <summary>
    /// Gets or sets the host-side forwarded port.
    /// </summary>
    public int HostPort { get; init; } = RemoteControlProtocol.DefaultPort;

    /// <summary>
    /// Gets or sets the bearer token. If absent, package marker discovery may supply it.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// Gets or sets the transport protocol for explicit device-port connections.
    /// </summary>
    public string TransportProtocol { get; init; } = RemoteControlProtocol.GrpcTransportProtocol;

    /// <summary>
    /// Gets or sets whether a stopped Android package should be launched before marker discovery.
    /// </summary>
    public bool LaunchPackageIfStopped { get; init; }

    /// <summary>
    /// Gets or sets whether the discovered connection profile should be saved.
    /// </summary>
    public bool SaveProfile { get; init; }

    /// <summary>
    /// Gets or sets whether the forward should be removed when the one-shot CLI command exits.
    /// </summary>
    public bool CleanupOnExit { get; init; } = true;

    /// <summary>
    /// Gets or sets the maximum time to wait for a launched package to report a process ID.
    /// </summary>
    public TimeSpan PackageStartTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets or sets the polling interval used while waiting for a launched package.
    /// </summary>
    public TimeSpan PackageStartPollInterval { get; init; } = TimeSpan.FromMilliseconds(500);
}
