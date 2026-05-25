using Avalonia.RemoteControl.Client.Live;
using Avalonia.RemoteControl.Protocol.V1;

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
}
