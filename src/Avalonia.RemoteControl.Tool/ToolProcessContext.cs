namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Captures process-level startup values that should remain stable for the GUI lifetime.
/// </summary>
public static class ToolProcessContext
{
    /// <summary>
    /// Environment variable containing semicolon-delimited workspace roots to search when the launch directory is stale.
    /// </summary>
    public const string WorkspaceRootsEnvironmentVariable = "AVALONIA_REMOTE_CONTROL_WORKSPACE_ROOTS";

    private static string startupWorkingDirectory = ResolveStartupWorkingDirectory(Environment.CurrentDirectory);

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
        startupWorkingDirectory = ResolveStartupWorkingDirectory(workingDirectory);
    }

    /// <summary>
    /// Resolves a launch working directory, redirecting stale non-repository folders to a matching checkout when possible.
    /// </summary>
    /// <param name="workingDirectory">The working directory to resolve, or the current process directory when omitted.</param>
    /// <returns>The resolved working directory.</returns>
    public static string ResolveStartupWorkingDirectory(string? workingDirectory = null)
    {
        var normalized = NormalizeWorkingDirectory(
            string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory);

        if (IsInsideGitRepository(normalized))
        {
            return normalized;
        }

        foreach (var directoryName in EnumerateDirectoryNames(normalized))
        {
            foreach (var workspaceRoot in EnumerateWorkspaceRoots())
            {
                var candidate = Path.Combine(workspaceRoot, directoryName);
                if (IsGitRepositoryRoot(candidate))
                {
                    return NormalizeWorkingDirectory(candidate);
                }
            }
        }

        return normalized;
    }

    internal static string NormalizeWorkingDirectory(string workingDirectory, string? basePath = null)
    {
        var trimmed = workingDirectory.Trim();
        return !string.IsNullOrWhiteSpace(basePath) && !Path.IsPathRooted(trimmed)
            ? Path.GetFullPath(trimmed, basePath)
            : Path.GetFullPath(trimmed);
    }

    private static bool IsInsideGitRepository(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            return false;
        }

        var current = new DirectoryInfo(directory);
        while (current is not null)
        {
            if (IsGitRepositoryRoot(current.FullName))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsGitRepositoryRoot(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        // A standard repository has .git/HEAD; a worktree or submodule uses a .git pointer file.
        // A stray or invalid .git directory without HEAD (for example a leftover folder in the user
        // home) must not be treated as a repository root, or it would suppress stale-folder redirect.
        return File.Exists(Path.Combine(directory, ".git", "HEAD")) ||
            File.Exists(Path.Combine(directory, ".git"));
    }

    private static IEnumerable<string> EnumerateDirectoryNames(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current is not null && !string.IsNullOrWhiteSpace(current.Name))
        {
            yield return current.Name;
            current = current.Parent;
        }
    }

    private static IEnumerable<string> EnumerateWorkspaceRoots()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var configuredRoots = Environment.GetEnvironmentVariable(WorkspaceRootsEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredRoots))
        {
            foreach (var root in configuredRoots.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryNormalizeExistingDirectory(root, seen, out var normalized))
                {
                    yield return normalized;
                }
            }
        }

        foreach (var root in EnumerateDefaultWorkspaceRoots())
        {
            if (TryNormalizeExistingDirectory(root, seen, out var normalized))
            {
                yield return normalized;
            }
        }
    }

    private static IEnumerable<string> EnumerateDefaultWorkspaceRoots()
    {
        yield return @"F:\GitHub";
        yield return @"C:\GitHub";

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            yield return Path.Combine(userProfile, "source", "repos");
        }
    }

    private static bool TryNormalizeExistingDirectory(
        string directory,
        HashSet<string> seen,
        out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return false;
        }

        normalized = NormalizeWorkingDirectory(directory);
        return seen.Add(normalized);
    }
}
