namespace Avalonia.RemoteControl.Client;

/// <summary>
/// Identifies the supported client connection profiles.
/// </summary>
public enum RemoteControlConnectionMode
{
    /// <summary>
    /// Connect to a local process through loopback.
    /// </summary>
    Local,

    /// <summary>
    /// Connect to a LAN endpoint protected by TLS.
    /// </summary>
    Network,

    /// <summary>
    /// Connect to an Android emulator or attached device through ADB forwarding.
    /// </summary>
    Adb
}
