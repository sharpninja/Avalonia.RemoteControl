using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Live;

/// <summary>
/// Tracks live tree state for the remote view window.
/// </summary>
public sealed class RemoteLiveTreeModel
{
    private readonly Dictionary<string, TreeNode> nodesById = new(StringComparer.Ordinal);
    private readonly List<TreeNode> nodes = [];

    /// <summary>
    /// Gets the latest snapshot sequence.
    /// </summary>
    public ulong Sequence { get; private set; }

    /// <summary>
    /// Gets the selected remote node ID.
    /// </summary>
    public string? SelectedNodeId { get; private set; }

    /// <summary>
    /// Gets the selected node when it is present in the latest snapshot.
    /// </summary>
    public TreeNode? SelectedNode =>
        SelectedNodeId is not null && nodesById.TryGetValue(SelectedNodeId, out var node)
            ? node
            : null;

    /// <summary>
    /// Gets all nodes from the latest snapshot.
    /// </summary>
    public IReadOnlyCollection<TreeNode> Nodes => nodes;

    /// <summary>
    /// Applies a live tree snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot to apply.</param>
    public void ApplySnapshot(TreeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Sequence = snapshot.Sequence;
        nodesById.Clear();
        nodes.Clear();

        foreach (var node in snapshot.Nodes)
        {
            nodesById[node.Id] = node;
            nodes.Add(node);
        }

        if (SelectedNodeId is not null && !nodesById.ContainsKey(SelectedNodeId))
        {
            SelectedNodeId = null;
        }
    }

    /// <summary>
    /// Selects a node by ID.
    /// </summary>
    /// <param name="nodeId">Node ID.</param>
    public void SelectNode(string? nodeId)
    {
        SelectedNodeId = nodeId is not null && nodesById.ContainsKey(nodeId)
            ? nodeId
            : null;
    }

    /// <summary>
    /// Finds the visible node at the supplied root-relative point.
    /// </summary>
    /// <param name="x">Root-relative X coordinate in DIPs.</param>
    /// <param name="y">Root-relative Y coordinate in DIPs.</param>
    /// <returns>The deepest matching node, or <see langword="null" />.</returns>
    public TreeNode? HitTest(double x, double y)
    {
        var bestIndex = -1;
        var bestDepth = -1;
        TreeNode? best = null;

        for (var index = 0; index < nodes.Count; index++)
        {
            var node = nodes[index];

            if (!node.IsVisible || !Contains(node, x, y))
            {
                continue;
            }

            var depth = GetDepth(node);
            if (depth > bestDepth || (depth == bestDepth && index > bestIndex))
            {
                best = node;
                bestDepth = depth;
                bestIndex = index;
            }
        }

        return best;
    }

    private int GetDepth(TreeNode node)
    {
        var depth = 0;
        var current = node;

        while (!string.IsNullOrWhiteSpace(current.ParentId)
            && nodesById.TryGetValue(current.ParentId, out var parent))
        {
            depth++;
            current = parent;
        }

        return depth;
    }

    private static bool Contains(TreeNode node, double x, double y)
    {
        var bounds = node.AbsoluteBounds;

        return bounds.Width > 0
            && bounds.Height > 0
            && x >= bounds.X
            && y >= bounds.Y
            && x <= bounds.X + bounds.Width
            && y <= bounds.Y + bounds.Height;
    }
}
