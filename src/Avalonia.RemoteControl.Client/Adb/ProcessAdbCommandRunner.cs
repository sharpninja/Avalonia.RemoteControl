using System.Diagnostics;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Runs adb as an external process.
/// </summary>
public sealed class ProcessAdbCommandRunner : IAdbCommandRunner
{
    private readonly string adbPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessAdbCommandRunner"/> class.
    /// </summary>
    /// <param name="adbPath">Optional adb executable path. Defaults to PATH lookup or AVALONIA_REMOTE_ADB_PATH.</param>
    public ProcessAdbCommandRunner(string? adbPath = null)
    {
        this.adbPath = string.IsNullOrWhiteSpace(adbPath)
            ? Environment.GetEnvironmentVariable("AVALONIA_REMOTE_ADB_PATH") ?? "adb"
            : adbPath;
    }

    /// <inheritdoc />
    public async Task<AdbCommandResult> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = adbPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start adb.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new AdbCommandResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(false),
            await standardError.ConfigureAwait(false));
    }
}
