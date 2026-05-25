namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Represents the result of an adb process invocation.
/// </summary>
public sealed record AdbCommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    /// <summary>
    /// Gets a successful empty adb command result.
    /// </summary>
    public static AdbCommandResult Success { get; } = new(0, string.Empty, string.Empty);
}
