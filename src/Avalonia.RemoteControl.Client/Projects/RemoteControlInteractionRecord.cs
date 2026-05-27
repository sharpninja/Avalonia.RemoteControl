namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Kind of remote-control interaction recorded for replay.
/// </summary>
public enum RemoteControlInteractionKind
{
    /// <summary>
    /// Unknown interaction.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Click invocation.
    /// </summary>
    Click,

    /// <summary>
    /// Focus invocation.
    /// </summary>
    Focus,

    /// <summary>
    /// Property mutation.
    /// </summary>
    SetProperty,

    /// <summary>
    /// Live input batch.
    /// </summary>
    InputBatch,
}

/// <summary>
/// Recorded interaction in a replayable debugging session.
/// </summary>
public sealed class RemoteControlInteractionRecord
{
    /// <summary>
    /// Gets or sets the stable step identifier.
    /// </summary>
    public string StepId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the replay order.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the interaction kind.
    /// </summary>
    public RemoteControlInteractionKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the interaction timestamp.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the elapsed milliseconds since session start.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// Gets or sets the target remote node identifier.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property name for property mutations.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target property value for property mutations.
    /// </summary>
    public string PropertyValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets recorded live input events.
    /// </summary>
    public List<RemoteControlInputEventRecord> InputEvents { get; set; } = [];

    /// <summary>
    /// Gets fields whose payload values are sensitive.
    /// </summary>
    public List<string> SensitiveFields { get; set; } = [];

    /// <summary>
    /// Gets or sets the artifact identifier for the state captured before the interaction.
    /// </summary>
    public string BeforeSnapshotArtifactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artifact identifier for the state captured after the interaction.
    /// </summary>
    public string AfterSnapshotArtifactId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the original interaction succeeded.
    /// </summary>
    public bool ResultSucceeded { get; set; }

    /// <summary>
    /// Gets or sets the original sanitized command result message.
    /// </summary>
    public string ResultMessage { get; set; } = string.Empty;

    /// <summary>
    /// Returns a sanitized one-line summary that never includes sensitive payload values.
    /// </summary>
    /// <returns>A sanitized summary.</returns>
    public string ToSanitizedSummary()
    {
        var target = string.IsNullOrWhiteSpace(NodeId)
            ? "no-node"
            : NodeId;
        var detail = Kind switch
        {
            RemoteControlInteractionKind.SetProperty => string.IsNullOrWhiteSpace(PropertyName)
                ? "property"
                : $"property {PropertyName}",
            RemoteControlInteractionKind.InputBatch => $"input events {InputEvents.Count}",
            _ => target,
        };
        var sensitive = SensitiveFields.Count == 0
            ? "no sensitive fields"
            : $"sensitive fields: {string.Join(", ", SensitiveFields)}";

        return $"Step {Order} {Kind}: {detail}; {sensitive}.";
    }
}

/// <summary>
/// Recorded live input event used by replay.
/// </summary>
public sealed record RemoteControlInputEventRecord
{
    /// <summary>
    /// Gets or sets the remote input kind name.
    /// </summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the root-relative X coordinate.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets or sets the root-relative Y coordinate.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets or sets the mouse button name.
    /// </summary>
    public string Button { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the wheel delta X value.
    /// </summary>
    public double DeltaX { get; init; }

    /// <summary>
    /// Gets or sets the wheel delta Y value.
    /// </summary>
    public double DeltaY { get; init; }

    /// <summary>
    /// Gets or sets the key name.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets typed text. This value is sensitive when <see cref="IsSensitive"/> is true.
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the original input timestamp.
    /// </summary>
    public ulong Timestamp { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this input contains sensitive payload values.
    /// </summary>
    public bool IsSensitive { get; init; }
}
