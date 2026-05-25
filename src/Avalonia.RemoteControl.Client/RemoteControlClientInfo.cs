using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Client;

/// <summary>
/// Provides client package metadata used by the tool launcher and tests.
/// </summary>
public static class RemoteControlClientInfo
{
    /// <summary>
    /// Gets the client command name.
    /// </summary>
    public const string CommandName = "avalonia-remote";

    /// <summary>
    /// Gets the supported connection modes.
    /// </summary>
    public static IReadOnlyList<RemoteControlConnectionMode> SupportedConnectionModes { get; } =
    [
        RemoteControlConnectionMode.Local,
        RemoteControlConnectionMode.Network,
        RemoteControlConnectionMode.Adb
    ];

    /// <summary>
    /// Creates launcher help text for the current tool surface.
    /// </summary>
    /// <returns>Help text suitable for console output.</returns>
    public static string CreateHelpText()
    {
        return $"""
        {CommandName} - Avalonia Remote Control client

        Protocol: {RemoteControlProtocol.DisplayVersion}

        Usage:
          {CommandName} --help
          {CommandName} adb list
          {CommandName} adb connect --serial <serial> --package <package>
          {CommandName} adb connect --serial <serial> --device-port <port> --token <token>
          {CommandName} adb cleanup --serial <serial> [--host-port <port>]

        Connection modes:
          local    Loopback debuggee connection.
          network  LAN/TLS debuggee connection.
          adb      Android emulator/device connection through adb forward.

        Current status:
          Early implementation build. ADB list/connect/cleanup are available; full client UI is planned.
        """;
    }
}
