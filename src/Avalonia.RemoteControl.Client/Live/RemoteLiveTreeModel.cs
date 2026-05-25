using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Live;

/// <summary>
/// Tracks live tree state for the remote view window.
/// </summary>
public sealed class RemoteLiveTreeModel
{
    private readonly Dictionary<string, TreeNode> nodesById = new(StringComparer.Ordinal);

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
    public IReadOnlyCollection<TreeNode> Nodes => nodesById.Values;

    /// <summary>
    /// Applies a live tree snapshot.
    /// </summary>
    /// <param name="snapshot">Snapshot to apply.</param>
    public void ApplySnapshot(TreeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Sequence = snapshot.Sequence;
        nodesById.Clear();

        foreach (var node in snapshot.Nodes)
        {
            nodesById[node.Id] = node;
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
}
