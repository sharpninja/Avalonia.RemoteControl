namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Runs adb commands for the client.
/// </summary>
public interface IAdbCommandRunner
{
    /// <summary>
    /// Runs adb with the supplied argument list.
    /// </summary>
    /// <param name="arguments">Arguments passed to adb without shell expansion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The adb process result.</returns>
    Task<AdbCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}
