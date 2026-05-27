using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Logging;

/// <summary>
/// Formats remote log entries for compact desktop client display.
/// </summary>
public static class RemoteLogDisplayFormatter
{
    /// <summary>
    /// Formats a remote log entry for the desktop log list.
    /// </summary>
    /// <param name="entry">The protocol log entry.</param>
    /// <returns>A single-line display string.</returns>
    public static string Format(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var prefixParts = new List<string>
        {
            $"#{entry.Sequence}",
        };

        AddIfPresent(prefixParts, entry.TimestampUtc);
        AddIfPresent(prefixParts, entry.Level);
        AddIfPresent(prefixParts, entry.Category);

        if (entry.EventId != 0)
        {
            prefixParts.Add($"event={entry.EventId}");
        }

        var detailParts = new List<string>();

        if (entry.DroppedCount > 0)
        {
            detailParts.Add($"dropped={entry.DroppedCount}");
        }

        AddNamedIfPresent(detailParts, "exception", entry.ExceptionSummary);
        AddNamedIfPresent(detailParts, "state", entry.StructuredState);
        AddNamedIfPresent(detailParts, "scope", entry.ScopeSummary);

        var message = string.IsNullOrWhiteSpace(entry.Message)
            ? "(no message)"
            : entry.Message.Trim();

        return detailParts.Count == 0
            ? $"{string.Join(" ", prefixParts)}: {message}"
            : $"{string.Join(" ", prefixParts)}: {message} ({string.Join("; ", detailParts)})";
    }

    private static void AddIfPresent(ICollection<string> parts, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value.Trim());
        }
    }

    private static void AddNamedIfPresent(ICollection<string> parts, string name, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{name}={value.Trim()}");
        }
    }
}
