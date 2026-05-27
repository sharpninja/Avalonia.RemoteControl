using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Replay target backed by a live <see cref="RemoteControlDesktopSession"/>.
/// </summary>
public sealed class RemoteControlDesktopReplayTarget : IRemoteControlReplayTarget
{
    private readonly RemoteControlDesktopSession session;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlDesktopReplayTarget"/> class.
    /// </summary>
    /// <param name="session">Remote-control desktop session.</param>
    public RemoteControlDesktopReplayTarget(RemoteControlDesktopSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc />
    public async Task<RemoteControlReplayCommandResult> InvokeClickAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        var result = await session.InvokeClickAsync(nodeId, cancellationToken).ConfigureAwait(false);
        return FromCommandResult(result);
    }

    /// <inheritdoc />
    public async Task<RemoteControlReplayCommandResult> InvokeFocusAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        var result = await session.InvokeFocusAsync(nodeId, cancellationToken).ConfigureAwait(false);
        return FromCommandResult(result);
    }

    /// <inheritdoc />
    public async Task<RemoteControlReplayCommandResult> SetPropertyAsync(
        string nodeId,
        string propertyName,
        string value,
        CancellationToken cancellationToken = default)
    {
        var result = await session.SetPropertyAsync(
            nodeId,
            propertyName,
            value,
            cancellationToken).ConfigureAwait(false);
        return FromCommandResult(result);
    }

    /// <inheritdoc />
    public async Task<RemoteControlReplayCommandResult> SendInputAsync(
        IReadOnlyList<RemoteControlInputEventRecord> events,
        CancellationToken cancellationToken = default)
    {
        var protocolEvents = events.Select(ToProtocol).ToList();
        var result = await session.SendInputAsync(protocolEvents, cancellationToken).ConfigureAwait(false);
        return FromCommandResult(result);
    }

    /// <inheritdoc />
    public async Task<RemoteControlProjectTreeSnapshot> CaptureTreeSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshot = await session.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        return RemoteControlProjectTreeSnapshot.FromProtocol(snapshot);
    }

    private static RemoteControlReplayCommandResult FromCommandResult(CommandResult result)
    {
        return new RemoteControlReplayCommandResult(result.Succeeded, result.Message);
    }

    private static RemoteInputEvent ToProtocol(RemoteControlInputEventRecord record)
    {
        return new RemoteInputEvent
        {
            Kind = ParseEnum(record.Kind, RemoteInputKind.Unspecified),
            X = record.X,
            Y = record.Y,
            Button = ParseEnum(record.Button, RemoteMouseButton.Unspecified),
            DeltaX = record.DeltaX,
            DeltaY = record.DeltaY,
            Key = record.Key,
            Text = record.Text,
            Timestamp = record.Timestamp,
        };
    }

    private static T ParseEnum<T>(string value, T fallback)
        where T : struct
    {
        return !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }
}
