namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Represents an active adb forward created by the client.
/// </summary>
public sealed record AdbForward(
    string Serial,
    int HostPort,
    int DevicePort,
    Uri Endpoint);
