namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Target that can execute recorded remote-control interactions during replay.
/// </summary>
public interface IRemoteControlReplayTarget
{
    /// <summary>
    /// Replays a click interaction.
    /// </summary>
    /// <param name="nodeId">Target node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    Task<RemoteControlReplayCommandResult> InvokeClickAsync(
        string nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays a focus interaction.
    /// </summary>
    /// <param name="nodeId">Target node identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    Task<RemoteControlReplayCommandResult> InvokeFocusAsync(
        string nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays a property mutation.
    /// </summary>
    /// <param name="nodeId">Target node identifier.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="value">Property value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    Task<RemoteControlReplayCommandResult> SetPropertyAsync(
        string nodeId,
        string propertyName,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays a live input batch.
    /// </summary>
    /// <param name="events">Input events.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    Task<RemoteControlReplayCommandResult> SendInputAsync(
        IReadOnlyList<RemoteControlInputEventRecord> events,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures current tree state after a replayed interaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Captured tree snapshot.</returns>
    Task<RemoteControlProjectTreeSnapshot> CaptureTreeSnapshotAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sanitized command result returned by replay targets.
/// </summary>
/// <param name="Succeeded">Whether the command succeeded.</param>
/// <param name="Message">Sanitized message.</param>
public sealed record RemoteControlReplayCommandResult(bool Succeeded, string Message)
{
    /// <summary>
    /// Creates a successful command result.
    /// </summary>
    /// <param name="message">Sanitized message.</param>
    /// <returns>Command result.</returns>
    public static RemoteControlReplayCommandResult Success(string message) =>
        new(true, message);

    /// <summary>
    /// Creates a failed command result.
    /// </summary>
    /// <param name="message">Sanitized message.</param>
    /// <returns>Command result.</returns>
    public static RemoteControlReplayCommandResult Failure(string message) =>
        new(false, message);
}
