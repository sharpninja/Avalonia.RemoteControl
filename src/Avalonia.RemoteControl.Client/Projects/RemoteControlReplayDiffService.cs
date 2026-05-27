namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Kind of node difference found during replay comparison.
/// </summary>
public enum RemoteControlReplayDiffKind
{
    /// <summary>
    /// Node is present and unchanged.
    /// </summary>
    Unchanged = 0,

    /// <summary>
    /// Node was added in the replayed state.
    /// </summary>
    Added,

    /// <summary>
    /// Node was removed from the replayed state.
    /// </summary>
    Removed,

    /// <summary>
    /// Node exists in both states but its captured state changed.
    /// </summary>
    Changed,
}

/// <summary>
/// Compares original and replayed tree snapshots.
/// </summary>
public static class RemoteControlReplayDiffService
{
    /// <summary>
    /// Compares two tree snapshots.
    /// </summary>
    /// <param name="original">Original captured snapshot.</param>
    /// <param name="replayed">Replay-time snapshot.</param>
    /// <returns>A replay diff.</returns>
    public static RemoteControlReplayDiff CompareTreeSnapshots(
        RemoteControlProjectTreeSnapshot? original,
        RemoteControlProjectTreeSnapshot? replayed)
    {
        if (original is null || replayed is null)
        {
            return RemoteControlReplayDiff.Empty;
        }

        var originalById = original.Nodes
            .Where(static node => !string.IsNullOrWhiteSpace(node.Id))
            .ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var replayedById = replayed.Nodes
            .Where(static node => !string.IsNullOrWhiteSpace(node.Id))
            .ToDictionary(static node => node.Id, StringComparer.Ordinal);
        var diffs = new List<RemoteControlReplayNodeDiff>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var originalNode in original.Nodes)
        {
            if (string.IsNullOrWhiteSpace(originalNode.Id))
            {
                continue;
            }

            seen.Add(originalNode.Id);

            if (!replayedById.TryGetValue(originalNode.Id, out var replayedNode))
            {
                diffs.Add(RemoteControlReplayNodeDiff.Removed(originalNode.Id));
                continue;
            }

            var changes = CompareNode(originalNode, replayedNode);
            diffs.Add(changes.Count == 0
                ? RemoteControlReplayNodeDiff.Unchanged(originalNode.Id)
                : RemoteControlReplayNodeDiff.Changed(originalNode.Id, changes));
        }

        foreach (var replayedNode in replayed.Nodes)
        {
            if (!string.IsNullOrWhiteSpace(replayedNode.Id) && !seen.Contains(replayedNode.Id))
            {
                diffs.Add(RemoteControlReplayNodeDiff.Added(replayedNode.Id));
            }
        }

        return new RemoteControlReplayDiff(diffs);
    }

    private static List<RemoteControlReplayPropertyChange> CompareNode(
        RemoteControlProjectTreeNode original,
        RemoteControlProjectTreeNode replayed)
    {
        var changes = new List<RemoteControlReplayPropertyChange>();

        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.ParentId), original.ParentId, replayed.ParentId);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.TypeName), original.TypeName, replayed.TypeName);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.Name), original.Name, replayed.Name);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.AutomationId), original.AutomationId, replayed.AutomationId);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.AutomationName), original.AutomationName, replayed.AutomationName);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.IsVisible), original.IsVisible, replayed.IsVisible);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.IsEnabled), original.IsEnabled, replayed.IsEnabled);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.IsFocused), original.IsFocused, replayed.IsFocused);
        AddIfChanged(changes, nameof(RemoteControlProjectTreeNode.Bounds), Format(original.Bounds), Format(replayed.Bounds));
        AddIfChanged(
            changes,
            nameof(RemoteControlProjectTreeNode.AbsoluteBounds),
            Format(original.AbsoluteBounds),
            Format(replayed.AbsoluteBounds));
        AddIfChanged(
            changes,
            nameof(RemoteControlProjectTreeNode.Classes),
            string.Join("|", original.Classes),
            string.Join("|", replayed.Classes));

        var originalProperties = original.Properties.ToDictionary(static item => item.Name, StringComparer.Ordinal);
        var replayedProperties = replayed.Properties.ToDictionary(static item => item.Name, StringComparer.Ordinal);
        foreach (var (name, originalProperty) in originalProperties)
        {
            var replayedValue = replayedProperties.TryGetValue(name, out var replayedProperty)
                ? replayedProperty.Value
                : "<missing>";
            AddIfChanged(changes, $"Property:{name}", originalProperty.Value, replayedValue);
        }

        foreach (var name in replayedProperties.Keys.Where(name => !originalProperties.ContainsKey(name)))
        {
            AddIfChanged(changes, $"Property:{name}", "<missing>", replayedProperties[name].Value);
        }

        return changes;
    }

    private static void AddIfChanged<T>(
        ICollection<RemoteControlReplayPropertyChange> changes,
        string propertyName,
        T original,
        T replayed)
    {
        var originalValue = original?.ToString() ?? string.Empty;
        var replayedValue = replayed?.ToString() ?? string.Empty;
        if (!string.Equals(originalValue, replayedValue, StringComparison.Ordinal))
        {
            changes.Add(new RemoteControlReplayPropertyChange(propertyName, originalValue, replayedValue));
        }
    }

    private static string Format(RemoteControlProjectRect rect)
    {
        return $"{rect.X},{rect.Y},{rect.Width},{rect.Height}";
    }
}

/// <summary>
/// Replay diff for a single state comparison.
/// </summary>
/// <param name="NodeDiffs">Node differences.</param>
public sealed record RemoteControlReplayDiff(IReadOnlyList<RemoteControlReplayNodeDiff> NodeDiffs)
{
    /// <summary>
    /// Gets an empty replay diff.
    /// </summary>
    public static RemoteControlReplayDiff Empty { get; } = new([]);
}

/// <summary>
/// Difference for a single node.
/// </summary>
/// <param name="NodeId">Node identifier.</param>
/// <param name="Kind">Diff kind.</param>
/// <param name="Changes">Changed properties.</param>
public sealed record RemoteControlReplayNodeDiff(
    string NodeId,
    RemoteControlReplayDiffKind Kind,
    IReadOnlyList<RemoteControlReplayPropertyChange> Changes)
{
    /// <summary>
    /// Creates an added-node diff.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <returns>A node diff.</returns>
    public static RemoteControlReplayNodeDiff Added(string nodeId) =>
        new(nodeId, RemoteControlReplayDiffKind.Added, []);

    /// <summary>
    /// Creates a removed-node diff.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <returns>A node diff.</returns>
    public static RemoteControlReplayNodeDiff Removed(string nodeId) =>
        new(nodeId, RemoteControlReplayDiffKind.Removed, []);

    /// <summary>
    /// Creates an unchanged-node diff.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <returns>A node diff.</returns>
    public static RemoteControlReplayNodeDiff Unchanged(string nodeId) =>
        new(nodeId, RemoteControlReplayDiffKind.Unchanged, []);

    /// <summary>
    /// Creates a changed-node diff.
    /// </summary>
    /// <param name="nodeId">Node identifier.</param>
    /// <param name="changes">Changed properties.</param>
    /// <returns>A node diff.</returns>
    public static RemoteControlReplayNodeDiff Changed(
        string nodeId,
        IReadOnlyList<RemoteControlReplayPropertyChange> changes) =>
        new(nodeId, RemoteControlReplayDiffKind.Changed, changes);
}

/// <summary>
/// Changed property in a replay diff.
/// </summary>
/// <param name="PropertyName">Property name.</param>
/// <param name="OriginalValue">Original value.</param>
/// <param name="ReplayedValue">Replayed value.</param>
public sealed record RemoteControlReplayPropertyChange(
    string PropertyName,
    string OriginalValue,
    string ReplayedValue);
