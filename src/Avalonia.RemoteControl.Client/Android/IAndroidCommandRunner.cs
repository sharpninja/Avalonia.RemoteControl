using Avalonia.RemoteControl.Client.Adb;

namespace Avalonia.RemoteControl.Client.Android;

/// <summary>
/// Runs Android SDK commands that are not direct adb operations.
/// </summary>
public interface IAndroidCommandRunner
{
    /// <summary>
    /// Runs an Android SDK command and waits for completion.
    /// </summary>
    /// <param name="fileName">Executable path or command name.</param>
    /// <param name="arguments">Arguments passed without shell expansion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    Task<AdbCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts an Android SDK command and returns immediately.
    /// </summary>
    /// <param name="fileName">Executable path or command name.</param>
    /// <param name="arguments">Arguments passed without shell expansion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Started process metadata.</returns>
    Task<AndroidStartedProcess> StartAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
