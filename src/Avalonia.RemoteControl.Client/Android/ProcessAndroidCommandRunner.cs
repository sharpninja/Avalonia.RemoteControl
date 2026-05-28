using System.Diagnostics;
using Avalonia.RemoteControl.Client.Adb;

namespace Avalonia.RemoteControl.Client.Android;

/// <summary>
/// Runs Android SDK commands as external processes.
/// </summary>
public sealed class ProcessAndroidCommandRunner : IAndroidCommandRunner
{
    /// <inheritdoc />
    public async Task<AdbCommandResult> RunAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);

        var startInfo = CreateStartInfo(fileName, arguments);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start '{fileName}'.");

        var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardError = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new AdbCommandResult(
            process.ExitCode,
            await standardOutput.ConfigureAwait(false),
            await standardError.ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task<AndroidStartedProcess> StartAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(arguments);
        cancellationToken.ThrowIfCancellationRequested();

        var startInfo = CreateStartInfo(fileName, arguments);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start '{fileName}'.");

        return Task.FromResult(new AndroidStartedProcess(
            process.Id,
            fileName,
            [.. arguments]));
    }

    private static ProcessStartInfo CreateStartInfo(
        string fileName,
        IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
