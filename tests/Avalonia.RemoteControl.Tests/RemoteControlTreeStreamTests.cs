using Avalonia.Controls;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlTreeStreamTests
{
    [Fact]
    public async Task TreeStreamEmitsSnapshotsUntilCanceled()
    {
        var root = new StackPanel();
        root.Children.Add(new TextBlock { Text = "Live" });
        var provider = new AvaloniaControlTreeSnapshotProvider(
            Options.Create(new AvaloniaRemoteControlOptions()),
            new InlineRemoteControlDispatcher());
        var options = new AvaloniaRemoteControlOptions
        {
            TreeStreamInterval = TimeSpan.FromMilliseconds(1),
        };
        var stream = new RemoteControlTreeStreamService(
            provider,
            new StaticRemoteControlRootProvider(root),
            Options.Create(options));
        using var cancellation = new CancellationTokenSource();

        await using var enumerator = stream.WatchSnapshotsAsync(cancellation.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(2, enumerator.Current.Nodes.Count);

        cancellation.Cancel();
        Assert.False(await enumerator.MoveNextAsync());
    }
}
