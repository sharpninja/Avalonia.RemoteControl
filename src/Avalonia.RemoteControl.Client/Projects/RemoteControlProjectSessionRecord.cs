using Avalonia.RemoteControl.Client.Profiles;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Recorded client debugging session within a project.
/// </summary>
public sealed class RemoteControlProjectSessionRecord
{
    /// <summary>
    /// Gets or sets the stable session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the associated app identifier.
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the app display name at the time of connection.
    /// </summary>
    public string AppDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the endpoint URI text used by the session.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the transport protocol used by the session.
    /// </summary>
    public string TransportProtocol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection mode used by the session.
    /// </summary>
    public string ConnectionMode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authenticated audit identity reported by endpoint capabilities.
    /// </summary>
    public string AuthenticatedClientIdentity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session start timestamp.
    /// </summary>
    public DateTimeOffset StartedUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the optional session end timestamp.
    /// </summary>
    public DateTimeOffset? EndedUtc { get; set; }

    /// <summary>
    /// Gets recorded log rows and remote log entries.
    /// </summary>
    public List<RemoteControlProjectLogRecord> Logs { get; set; } = [];

    /// <summary>
    /// Gets recorded remote-control interactions.
    /// </summary>
    public List<RemoteControlInteractionRecord> Interactions { get; set; } = [];

    /// <summary>
    /// Gets replay artifacts associated with the session.
    /// </summary>
    public List<RemoteControlReplayArtifact> Artifacts { get; set; } = [];

    /// <summary>
    /// Starts a session record from a connection profile.
    /// </summary>
    /// <param name="sessionId">Stable session identifier.</param>
    /// <param name="appId">App identifier.</param>
    /// <param name="profile">Connection profile.</param>
    /// <param name="startedUtc">Start timestamp.</param>
    /// <returns>A session record.</returns>
    public static RemoteControlProjectSessionRecord Start(
        string sessionId,
        string appId,
        RemoteControlConnectionProfile profile,
        DateTimeOffset startedUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);
        ArgumentNullException.ThrowIfNull(profile);

        return new RemoteControlProjectSessionRecord
        {
            SessionId = sessionId,
            AppId = appId,
            AppDisplayName = string.IsNullOrWhiteSpace(profile.DisplayName)
                ? appId
                : profile.DisplayName,
            Endpoint = profile.Endpoint,
            TransportProtocol = profile.TransportProtocol,
            ConnectionMode = profile.ConnectionMode,
            StartedUtc = startedUtc,
        };
    }

    /// <summary>
    /// Marks the session as ended.
    /// </summary>
    /// <param name="endedUtc">End timestamp.</param>
    public void Complete(DateTimeOffset? endedUtc = null)
    {
        EndedUtc = endedUtc ?? DateTimeOffset.UtcNow;
    }
}
