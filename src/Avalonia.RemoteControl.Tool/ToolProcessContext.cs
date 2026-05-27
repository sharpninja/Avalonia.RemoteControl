namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Captures process-level startup values that should remain stable for the GUI lifetime.
/// </summary>
public static class ToolProcessContext
{
    private static string startupWorkingDirectory = NormalizeWorkingDirectory(Environment.CurrentDirectory);

    /// <summary>
    /// Gets the working directory that was current when the tool process started.
    /// </summary>
    public static string StartupWorkingDirectory => startupWorkingDirectory;

    /// <summary>
    /// Captures the process startup working directory.
    /// </summary>
    /// <param name="workingDirectory">The working directory to capture, or the current process directory when omitted.</param>
    public static void CaptureStartupWorkingDirectory(string? workingDirectory = null)
    {
        startupWorkingDirectory = NormalizeWorkingDirectory(
            string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory);
    }

    internal static string NormalizeWorkingDirectory(string workingDirectory, string? basePath = null)
    {
        var trimmed = workingDirectory.Trim();
        return !string.IsNullOrWhiteSpace(basePath) && !Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed, basePath)
            : Path.GetFullPath(trimmed);
    }
}
