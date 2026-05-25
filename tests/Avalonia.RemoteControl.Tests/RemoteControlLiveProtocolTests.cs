using Avalonia.RemoteControl.Protocol.V1;
using Google.Protobuf;
using ProtocolRect = Avalonia.RemoteControl.Protocol.V1.Rect;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlLiveProtocolTests
{
    [Fact]
    public void LiveProtocolMessagesExposeFrameInputAndAbsoluteBounds()
    {
        var capabilities = new GetCapabilitiesResponse
        {
            SupportsFrameStreaming = true,
            SupportsRemoteInput = true,
        };
        var frame = new FrameUpdate
        {
            Sequence = 42,
            Png = ByteString.CopyFrom([0x89, 0x50, 0x4E, 0x47]),
            PixelWidth = 100,
            PixelHeight = 50,
            RootWidth = 200,
            RootHeight = 100,
            RenderScale = 0.5,
            TimestampUtc = "2026-05-25T21:00:00Z",
        };
        var input = new SendInputRequest
        {
            Events =
            {
                new RemoteInputEvent
                {
                    Kind = RemoteInputKind.PointerPress,
                    Button = RemoteMouseButton.Left,
                    X = 10,
                    Y = 20,
                },
                new RemoteInputEvent
                {
                    Kind = RemoteInputKind.Text,
                    Text = "x",
                },
            },
        };
        var node = new TreeNode
        {
            Bounds = new ProtocolRect { X = 1, Y = 2, Width = 3, Height = 4 },
            AbsoluteBounds = new ProtocolRect { X = 10, Y = 20, Width = 30, Height = 40 },
        };

        Assert.True(capabilities.SupportsFrameStreaming);
        Assert.True(capabilities.SupportsRemoteInput);
        Assert.Equal(42UL, frame.Sequence);
        Assert.Equal(100, frame.PixelWidth);
        Assert.Equal(2, input.Events.Count);
        Assert.Equal(RemoteInputKind.PointerPress, input.Events[0].Kind);
        Assert.Equal(10, node.AbsoluteBounds.X);
    }
}
