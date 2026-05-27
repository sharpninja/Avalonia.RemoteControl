namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Replays recorded session interactions against a connected remote app.
/// </summary>
public sealed class RemoteControlSessionReplayService
{
    /// <summary>
    /// Replays a recorded session against a target.
    /// </summary>
    /// <param name="session">Recorded project session.</param>
    /// <param name="target">Replay target.</param>
    /// <param name="options">Replay options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Replay result with per-step diffs.</returns>
    public async Task<RemoteControlReplayResult> ReplayAsync(
        RemoteControlProjectSessionRecord session,
        IRemoteControlReplayTarget target,
        RemoteControlReplayOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(target);

        options ??= RemoteControlReplayOptions.Default;
        var artifacts = session.Artifacts.ToDictionary(static item => item.ArtifactId, StringComparer.Ordinal);
        var steps = new List<RemoteControlReplayStepResult>();
        long previousElapsed = 0;

        foreach (var interaction in session.Interactions.OrderBy(static item => item.Order))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (options.RespectTiming && interaction.ElapsedMilliseconds > previousElapsed)
            {
                var delay = TimeSpan.FromMilliseconds(
                    (interaction.ElapsedMilliseconds - previousElapsed) * options.TimingScale);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            previousElapsed = interaction.ElapsedMilliseconds;
            var commandResult = await ExecuteInteractionAsync(
                interaction,
                target,
                cancellationToken).ConfigureAwait(false);
            var replayedSnapshot = await target.CaptureTreeSnapshotAsync(cancellationToken).ConfigureAwait(false);
            var originalSnapshot = artifacts.TryGetValue(interaction.AfterSnapshotArtifactId, out var artifact)
                ? artifact.TreeSnapshot
                : null;
            var diff = RemoteControlReplayDiffService.CompareTreeSnapshots(originalSnapshot, replayedSnapshot);

            steps.Add(new RemoteControlReplayStepResult(
                interaction.StepId,
                interaction.Order,
                interaction.Kind,
                commandResult.Succeeded,
                commandResult.Message,
                diff));
        }

        return new RemoteControlReplayResult(session.SessionId, steps);
    }

    private static Task<RemoteControlReplayCommandResult> ExecuteInteractionAsync(
        RemoteControlInteractionRecord interaction,
        IRemoteControlReplayTarget target,
        CancellationToken cancellationToken)
    {
        return interaction.Kind switch
        {
            RemoteControlInteractionKind.Click => target.InvokeClickAsync(interaction.NodeId, cancellationToken),
            RemoteControlInteractionKind.Focus => target.InvokeFocusAsync(interaction.NodeId, cancellationToken),
            RemoteControlInteractionKind.SetProperty => target.SetPropertyAsync(
                interaction.NodeId,
                interaction.PropertyName,
                interaction.PropertyValue,
                cancellationToken),
            RemoteControlInteractionKind.InputBatch => target.SendInputAsync(interaction.InputEvents, cancellationToken),
            _ => Task.FromResult(RemoteControlReplayCommandResult.Failure("Unsupported replay interaction.")),
        };
    }
}

/// <summary>
/// Options controlling replay behavior.
/// </summary>
public sealed record RemoteControlReplayOptions
{
    /// <summary>
    /// Gets default replay options.
    /// </summary>
    public static RemoteControlReplayOptions Default { get; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether original interaction timing should be respected.
    /// </summary>
    public bool RespectTiming { get; init; }

    /// <summary>
    /// Gets or sets the multiplier applied to recorded timing delays.
    /// </summary>
    public double TimingScale { get; init; } = 1;
}

/// <summary>
/// Replay result for a session.
/// </summary>
/// <param name="SessionId">Recorded session identifier.</param>
/// <param name="Steps">Per-step replay results.</param>
public sealed record RemoteControlReplayResult(
    string SessionId,
    IReadOnlyList<RemoteControlReplayStepResult> Steps);

/// <summary>
/// Replay result for one interaction.
/// </summary>
/// <param name="StepId">Step identifier.</param>
/// <param name="Order">Replay order.</param>
/// <param name="Kind">Interaction kind.</param>
/// <param name="CommandSucceeded">Whether the replay command succeeded.</param>
/// <param name="Message">Sanitized command message.</param>
/// <param name="Diff">State diff after replaying the step.</param>
public sealed record RemoteControlReplayStepResult(
    string StepId,
    int Order,
    RemoteControlInteractionKind Kind,
    bool CommandSucceeded,
    string Message,
    RemoteControlReplayDiff Diff);
