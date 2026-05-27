using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Persisted project session log entry.
/// </summary>
public sealed record RemoteControlProjectLogRecord
{
    /// <summary>
    /// Gets or sets the log sequence number when supplied by the debuggee.
    /// </summary>
    public ulong Sequence { get; init; }

    /// <summary>
    /// Gets or sets the record timestamp.
    /// </summary>
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string Level { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the log category.
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the event identifier.
    /// </summary>
    public int EventId { get; init; }

    /// <summary>
    /// Gets or sets the sanitized message text.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the sanitized exception summary.
    /// </summary>
    public string ExceptionSummary { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of dropped log entries reported by the debuggee.
    /// </summary>
    public ulong DroppedCount { get; init; }

    /// <summary>
    /// Gets or sets the sanitized structured state summary.
    /// </summary>
    public string StructuredState { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the sanitized scope summary.
    /// </summary>
    public string ScopeSummary { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the desktop display row.
    /// </summary>
    public string DisplayRow { get; init; } = string.Empty;

    /// <summary>
    /// Creates a project log record from a protocol log entry.
    /// </summary>
    /// <param name="entry">Protocol log entry.</param>
    /// <param name="displayRow">Desktop display row.</param>
    /// <returns>A project log record.</returns>
    public static RemoteControlProjectLogRecord FromProtocol(LogEntry entry, string displayRow)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new RemoteControlProjectLogRecord
        {
            Sequence = entry.Sequence,
            TimestampUtc = ParseTimestamp(entry.TimestampUtc),
            Level = entry.Level,
            Category = entry.Category,
            EventId = entry.EventId,
            Message = entry.Message,
            ExceptionSummary = entry.ExceptionSummary,
            DroppedCount = entry.DroppedCount,
            StructuredState = entry.StructuredState,
            ScopeSummary = entry.ScopeSummary,
            DisplayRow = displayRow,
        };
    }

    /// <summary>
    /// Creates a project log record from a client status row.
    /// </summary>
    /// <param name="displayRow">Desktop display row.</param>
    /// <param name="timestampUtc">Timestamp.</param>
    /// <returns>A project log record.</returns>
    public static RemoteControlProjectLogRecord FromDisplayRow(
        string displayRow,
        DateTimeOffset? timestampUtc = null)
    {
        return new RemoteControlProjectLogRecord
        {
            TimestampUtc = timestampUtc ?? DateTimeOffset.UtcNow,
            Level = "Client",
            Category = "Avalonia.RemoteControl.Client",
            Message = displayRow,
            DisplayRow = displayRow,
        };
    }

    private static DateTimeOffset ParseTimestamp(string value)
    {
        return DateTimeOffset.TryParse(value, out var timestamp)
            ? timestamp.ToUniversalTime()
            : DateTimeOffset.UtcNow;
    }
}
