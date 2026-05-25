namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Represents Android-side remote-control endpoint metadata.
/// </summary>
public sealed record AdbEndpointInfo(int DevicePort, string? Token);
