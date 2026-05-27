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
            ? ResolveDefaultAdbPath()
            : adbPath;
    }

    /// <summary>
    /// Resolves the default ADB executable from configuration, PATH, or common Android SDK locations.
    /// </summary>
    /// <returns>The resolved ADB path, or <c>adb</c> for normal process lookup.</returns>
    public static string ResolveDefaultAdbPath()
    {
        var configured = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_ADB_PATH");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (!string.IsNullOrWhiteSpace(pathValue))
        {
            foreach (var directory in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(directory.Trim(), "adb.exe");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        foreach (var candidate in EnumerateCommonAdbPaths())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return "adb";
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

    private static IEnumerable<string> EnumerateCommonAdbPaths()
    {
        foreach (var variable in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
        {
            var root = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(root))
            {
                yield return Path.Combine(root, "platform-tools", "adb.exe");
            }
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "Android", "Sdk", "platform-tools", "adb.exe");
        }

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "Android", "android-sdk", "platform-tools", "adb.exe");
        }
    }
}
