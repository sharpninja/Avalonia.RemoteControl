using Avalonia.RemoteControl.Client.Adb;

namespace Avalonia.RemoteControl.Client.Android;

/// <summary>
/// Locates Android SDK command-line tools from explicit configuration, environment variables, and common install paths.
/// </summary>
public static class AndroidSdkLocator
{
    /// <summary>
    /// Resolves the Android emulator executable.
    /// </summary>
    /// <param name="androidSdkPath">Optional Android SDK root path.</param>
    /// <returns>The emulator executable path, or <c>emulator</c> for normal PATH lookup.</returns>
    public static string ResolveEmulatorPath(string? androidSdkPath = null)
    {
        foreach (var root in EnumerateSdkRoots(androidSdkPath))
        {
            var candidate = Path.Combine(root, "emulator", ExecutableName("emulator"));
            if (File.Exists(candidate) || !string.IsNullOrWhiteSpace(androidSdkPath))
            {
                return candidate;
            }
        }

        return ExecutableName("emulator");
    }

    /// <summary>
    /// Resolves likely Android SDK root paths.
    /// </summary>
    /// <param name="androidSdkPath">Optional Android SDK root path.</param>
    /// <returns>Candidate SDK roots in resolution order.</returns>
    public static IEnumerable<string> EnumerateSdkRoots(string? androidSdkPath = null)
    {
        if (!string.IsNullOrWhiteSpace(androidSdkPath))
        {
            yield return androidSdkPath;
            yield break;
        }

        foreach (var variable in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
        {
            var root = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(root))
            {
                yield return root;
            }
        }

        var adbPath = ProcessAdbCommandRunner.ResolveDefaultAdbPath();
        var inferred = InferSdkRootFromAdbPath(adbPath);
        if (!string.IsNullOrWhiteSpace(inferred))
        {
            yield return inferred;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "Android", "Sdk");
        }

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "Android", "android-sdk");
        }
    }

    private static string? InferSdkRootFromAdbPath(string adbPath)
    {
        if (string.IsNullOrWhiteSpace(adbPath)
            || string.Equals(adbPath, "adb", StringComparison.OrdinalIgnoreCase)
            || string.Equals(adbPath, ExecutableName("adb"), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var platformTools = Path.GetDirectoryName(adbPath);
        if (string.IsNullOrWhiteSpace(platformTools)
            || !string.Equals(
                Path.GetFileName(platformTools),
                "platform-tools",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Path.GetDirectoryName(platformTools);
    }

    private static string ExecutableName(string name) =>
        OperatingSystem.IsWindows() ? $"{name}.exe" : name;
}
