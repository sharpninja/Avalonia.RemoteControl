namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Represents a device returned by adb devices.
/// </summary>
public sealed record AdbDevice(
    string Serial,
    string State,
    string? Product,
    string? Model,
    string? Device);
