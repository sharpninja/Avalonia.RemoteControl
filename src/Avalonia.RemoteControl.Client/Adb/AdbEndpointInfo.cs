using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Represents Android-side remote-control endpoint metadata.
/// </summary>
public sealed record AdbEndpointInfo(int DevicePort, string? Token, string Protocol, string? ProtocolVersion)
{
    /// <summary>
    /// Marker protocol value for the desktop-facing gRPC endpoint.
    /// </summary>
    public const string GrpcProtocol = RemoteControlProtocol.GrpcTransportProtocol;

    /// <summary>
    /// Marker protocol value reserved for the Android-compatible bridge transport.
    /// </summary>
    public const string AndroidBridgeProtocol = RemoteControlProtocol.AndroidBridgeTransportProtocol;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdbEndpointInfo"/> class with the legacy gRPC marker shape.
    /// </summary>
    /// <param name="devicePort">Android-side listener port.</param>
    /// <param name="token">Bearer token discovered from the marker.</param>
    public AdbEndpointInfo(int devicePort, string? token)
        : this(devicePort, token, GrpcProtocol, null)
    {
    }

    /// <summary>
    /// Gets a value indicating whether the marker describes the current gRPC ADB transport.
    /// </summary>
    public bool IsGrpcProtocol => Protocol.Equals(GrpcProtocol, StringComparison.OrdinalIgnoreCase);
}
