namespace Avalonia.RemoteControl.Client.Android;

/// <summary>
/// Describes an Android SDK process that was started and left running.
/// </summary>
/// <param name="ProcessId">Operating system process ID.</param>
/// <param name="FileName">Executable path or command name.</param>
/// <param name="Arguments">Arguments passed to the executable.</param>
public sealed record AndroidStartedProcess(
    int ProcessId,
    string FileName,
    IReadOnlyList<string> Arguments);
