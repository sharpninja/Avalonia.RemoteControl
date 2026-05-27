using System.Security.Cryptography;
using System.Text;
using Avalonia.RemoteControl.Client.Profiles;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Builds stable identifiers for project-scoped records.
/// </summary>
public static class RemoteControlProjectIds
{
    /// <summary>
    /// Gets the default project identifier used by the desktop tool.
    /// </summary>
    public const string DefaultProjectId = "default";

    /// <summary>
    /// Gets the default project display name used by the desktop tool.
    /// </summary>
    public const string DefaultProjectName = "Default";

    /// <summary>
    /// Returns an app identifier for a connection profile.
    /// </summary>
    /// <param name="profile">Connection profile.</param>
    /// <returns>A stable app identifier.</returns>
    public static string GetAppId(RemoteControlConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (!string.IsNullOrWhiteSpace(profile.AppId))
        {
            return profile.AppId.Trim();
        }

        if (!string.IsNullOrWhiteSpace(profile.AndroidPackageName))
        {
            return profile.AndroidPackageName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(profile.Endpoint))
        {
            return $"endpoint-{Hash(profile.Endpoint.Trim())}";
        }

        return $"app-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Creates a new session identifier.
    /// </summary>
    /// <returns>A stable session identifier.</returns>
    public static string NewSessionId()
    {
        return $"session-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}";
    }

    /// <summary>
    /// Creates a new artifact identifier.
    /// </summary>
    /// <param name="prefix">Artifact prefix.</param>
    /// <returns>A stable artifact identifier.</returns>
    public static string NewArtifactId(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        return $"{prefix}-{Guid.NewGuid():N}";
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }
}
