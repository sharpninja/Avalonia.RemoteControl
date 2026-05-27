using Avalonia.RemoteControl.Client.Profiles;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Versioned project document containing app profiles, sessions, logs, and replay artifacts.
/// </summary>
public sealed class RemoteControlProjectDocument
{
    /// <summary>
    /// Gets the current project document schema version.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Gets or sets the project document schema version.
    /// </summary>
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    /// <summary>
    /// Gets or sets the stable project identifier.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the persisted desktop client layout state.
    /// </summary>
    public RemoteControlClientLayoutState ClientLayout { get; set; } = new();

    /// <summary>
    /// Gets project-scoped app connection profiles.
    /// </summary>
    public List<RemoteControlConnectionProfile> AppProfiles { get; set; } = [];

    /// <summary>
    /// Gets recorded debugging sessions.
    /// </summary>
    public List<RemoteControlProjectSessionRecord> Sessions { get; set; } = [];

    /// <summary>
    /// Creates a new project document.
    /// </summary>
    /// <param name="projectId">Stable project identifier.</param>
    /// <param name="name">Display name.</param>
    /// <param name="createdUtc">Optional creation timestamp.</param>
    /// <returns>A new project document.</returns>
    public static RemoteControlProjectDocument Create(
        string projectId,
        string name,
        DateTimeOffset? createdUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var now = createdUtc ?? DateTimeOffset.UtcNow;
        return new RemoteControlProjectDocument
        {
            ProjectId = projectId,
            Name = name,
            CreatedUtc = now,
            UpdatedUtc = now,
        };
    }

    /// <summary>
    /// Adds or replaces an app profile by app identifier.
    /// </summary>
    /// <param name="profile">Connection profile.</param>
    /// <returns>The profile stored in the project document.</returns>
    public RemoteControlConnectionProfile UpsertAppProfile(RemoteControlConnectionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var appId = RemoteControlProjectIds.GetAppId(profile);
        var normalized = profile with
        {
            AppId = appId,
            DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? appId
                : profile.DisplayName.Trim(),
            UpdatedUtc = profile.UpdatedUtc == default
                ? DateTimeOffset.UtcNow
                : profile.UpdatedUtc,
        };

        var existingIndex = AppProfiles.FindIndex(
            item => string.Equals(item.AppId, appId, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            AppProfiles[existingIndex] = normalized;
        }
        else
        {
            AppProfiles.Add(normalized);
        }

        UpdatedUtc = DateTimeOffset.UtcNow;
        return normalized;
    }
}
