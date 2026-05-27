namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Kind of replay artifact stored with a project session.
/// </summary>
public enum RemoteControlReplayArtifactKind
{
    /// <summary>
    /// Unknown artifact kind.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Control-tree snapshot artifact.
    /// </summary>
    TreeSnapshot,
}

/// <summary>
/// Persisted artifact used to reproduce and diff a recorded session.
/// </summary>
public sealed class RemoteControlReplayArtifact
{
    /// <summary>
    /// Gets or sets the stable artifact identifier.
    /// </summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artifact kind.
    /// </summary>
    public RemoteControlReplayArtifactKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the capture timestamp.
    /// </summary>
    public DateTimeOffset CapturedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a captured control-tree snapshot.
    /// </summary>
    public RemoteControlProjectTreeSnapshot? TreeSnapshot { get; set; }

    /// <summary>
    /// Creates a tree snapshot artifact.
    /// </summary>
    /// <param name="artifactId">Artifact identifier.</param>
    /// <param name="snapshot">Tree snapshot.</param>
    /// <param name="capturedUtc">Optional capture timestamp.</param>
    /// <returns>A replay artifact.</returns>
    public static RemoteControlReplayArtifact FromTreeSnapshot(
        string artifactId,
        RemoteControlProjectTreeSnapshot snapshot,
        DateTimeOffset? capturedUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentNullException.ThrowIfNull(snapshot);

        return new RemoteControlReplayArtifact
        {
            ArtifactId = artifactId,
            Kind = RemoteControlReplayArtifactKind.TreeSnapshot,
            CapturedUtc = capturedUtc ?? DateTimeOffset.UtcNow,
            TreeSnapshot = snapshot,
        };
    }
}
