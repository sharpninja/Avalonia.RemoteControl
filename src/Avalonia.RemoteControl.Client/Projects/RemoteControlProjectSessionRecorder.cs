using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Mutates the active project document while a remote-control session is connected.
/// </summary>
public sealed class RemoteControlProjectSessionRecorder
{
    private readonly RemoteControlProjectDocument document;

    private RemoteControlProjectSessionRecorder(
        RemoteControlProjectDocument document,
        RemoteControlProjectSessionRecord session)
    {
        this.document = document;
        Session = session;
    }

    /// <summary>
    /// Gets the active session record.
    /// </summary>
    public RemoteControlProjectSessionRecord Session { get; }

    /// <summary>
    /// Starts recording a project session.
    /// </summary>
    /// <param name="document">Project document.</param>
    /// <param name="profile">Connection profile.</param>
    /// <param name="startedUtc">Optional start timestamp.</param>
    /// <returns>A session recorder.</returns>
    public static RemoteControlProjectSessionRecorder Start(
        RemoteControlProjectDocument document,
        RemoteControlConnectionProfile profile,
        DateTimeOffset? startedUtc = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(profile);

        var normalized = document.UpsertAppProfile(profile);
        var session = RemoteControlProjectSessionRecord.Start(
            RemoteControlProjectIds.NewSessionId(),
            normalized.AppId,
            normalized,
            startedUtc ?? DateTimeOffset.UtcNow);
        document.Sessions.Add(session);
        return new RemoteControlProjectSessionRecorder(document, session);
    }

    /// <summary>
    /// Adds a protocol log entry to the active session.
    /// </summary>
    /// <param name="entry">Protocol log entry.</param>
    /// <param name="displayRow">Desktop display row.</param>
    public void AddLog(LogEntry entry, string displayRow)
    {
        Session.Logs.Add(RemoteControlProjectLogRecord.FromProtocol(entry, displayRow));
        document.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a client status row to the active session.
    /// </summary>
    /// <param name="displayRow">Desktop display row.</param>
    public void AddClientLog(string displayRow)
    {
        Session.Logs.Add(RemoteControlProjectLogRecord.FromDisplayRow(displayRow));
        document.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Adds a tree snapshot artifact to the active session.
    /// </summary>
    /// <param name="prefix">Artifact identifier prefix.</param>
    /// <param name="snapshot">Protocol tree snapshot.</param>
    /// <returns>The artifact identifier.</returns>
    public string AddTreeSnapshotArtifact(string prefix, TreeSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentNullException.ThrowIfNull(snapshot);

        var artifactId = RemoteControlProjectIds.NewArtifactId(prefix);
        Session.Artifacts.Add(RemoteControlReplayArtifact.FromTreeSnapshot(
            artifactId,
            RemoteControlProjectTreeSnapshot.FromProtocol(snapshot)));
        document.UpdatedUtc = DateTimeOffset.UtcNow;
        return artifactId;
    }

    /// <summary>
    /// Adds a replayable interaction to the active session.
    /// </summary>
    /// <param name="interaction">Interaction record.</param>
    public void AddInteraction(RemoteControlInteractionRecord interaction)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        if (string.IsNullOrWhiteSpace(interaction.StepId))
        {
            interaction.StepId = $"step-{Guid.NewGuid():N}";
        }

        if (interaction.Order <= 0)
        {
            interaction.Order = Session.Interactions.Count + 1;
        }

        Session.Interactions.Add(interaction);
        document.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Completes the active session.
    /// </summary>
    /// <param name="endedUtc">Optional end timestamp.</param>
    public void Complete(DateTimeOffset? endedUtc = null)
    {
        Session.Complete(endedUtc);
        document.UpdatedUtc = DateTimeOffset.UtcNow;
    }
}
