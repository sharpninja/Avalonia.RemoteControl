using Avalonia.RemoteControl.Client.Live;
using Avalonia.RemoteControl.Protocol.V1;
using ProtocolRect = Avalonia.RemoteControl.Protocol.V1.Rect;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlLiveClientTests
{
    [Fact]
    public void ViewportMapperPreservesRemoteCoordinatesUnderLetterboxing()
    {
        var mapper = RemoteViewCoordinateMapper.Create(
            remoteWidth: 200,
            remoteHeight: 100,
            viewportWidth: 500,
            viewportHeight: 500);

        var remote = mapper.ToRemote(250, 250);

        Assert.Equal(100, remote.X, precision: 3);
        Assert.Equal(50, remote.Y, precision: 3);
    }

    [Fact]
    public void LiveTreeModelPreservesSelectionAcrossUpdates()
    {
        var model = new RemoteLiveTreeModel();
        model.ApplySnapshot(new TreeSnapshot
        {
            Sequence = 1,
            Nodes =
            {
                new TreeNode { Id = "node-1", TypeName = "Button", Name = "Before" },
            },
        });
        model.SelectNode("node-1");

        model.ApplySnapshot(new TreeSnapshot
        {
            Sequence = 2,
            Nodes =
            {
                new TreeNode { Id = "node-1", TypeName = "Button", Name = "After" },
            },
        });

        Assert.Equal("node-1", model.SelectedNodeId);
        Assert.Equal("After", model.SelectedNode?.Name);
    }

    [Fact]
    public void LiveTreeModelHitTestSelectsDeepestVisibleNode()
    {
        var model = new RemoteLiveTreeModel();
        model.ApplySnapshot(new TreeSnapshot
        {
            Sequence = 1,
            Nodes =
            {
                Node("root", null, 0, 0, 200, 200),
                Node("panel", "root", 10, 10, 100, 100),
                Node("button", "panel", 20, 20, 40, 40),
            },
        });

        var hit = model.HitTest(30, 30);

        Assert.NotNull(hit);
        Assert.Equal("button", hit.Id);
    }

    [Fact]
    public void LiveTreeModelHitTestIgnoresInvisibleAndOutOfBoundsNodes()
    {
        var model = new RemoteLiveTreeModel();
        model.ApplySnapshot(new TreeSnapshot
        {
            Sequence = 1,
            Nodes =
            {
                Node("root", null, 0, 0, 200, 200),
                Node("hidden", "root", 10, 10, 100, 100, isVisible: false),
            },
        });

        var hiddenHit = model.HitTest(20, 20);
        var noHit = model.HitTest(250, 250);

        Assert.NotNull(hiddenHit);
        Assert.Equal("root", hiddenHit.Id);
        Assert.Null(noHit);
    }

    [Fact]
    public void LiveViewCapabilitiesDoNotAssumeFramesOrInputForOlderEndpoints()
    {
        var capabilities = RemoteLiveViewCapabilities.FromProtocol(new GetCapabilitiesResponse
        {
            ProtocolVersion = "1.0",
            SupportsTreeSnapshots = true,
            SupportsTreeStreaming = true,
        });

        Assert.True(capabilities.SupportsTreeSnapshots);
        Assert.True(capabilities.SupportsTreeStreaming);
        Assert.False(capabilities.SupportsFrameStreaming);
        Assert.False(capabilities.SupportsRemoteInput);
    }

    private static TreeNode Node(
        string id,
        string? parentId,
        double x,
        double y,
        double width,
        double height,
        bool isVisible = true)
    {
        return new TreeNode
        {
            Id = id,
            ParentId = parentId ?? string.Empty,
            TypeName = "Control",
            IsVisible = isVisible,
            AbsoluteBounds = new ProtocolRect
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            },
        };
    }
}
