using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Input;
using Avalonia.RemoteControl.Server.Rendering;
using Avalonia.RemoteControl.Server.Runtime;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlLiveRuntimeTests
{
    [Fact]
    public async Task FrameStreamRejectsWhenDisabled()
    {
        var root = new Border();
        var service = new RemoteControlFrameStreamService(
            new StaticRemoteControlRootProvider(root),
            new StubFrameProvider(),
            Options.Create(new AvaloniaRemoteControlOptions()),
            NullLogger<RemoteControlFrameStreamService>.Instance);

        await using var enumerator = service.WatchFramesAsync(CancellationToken.None).GetAsyncEnumerator();

        var exception = await Assert.ThrowsAsync<RemoteControlRuntimeException>(() => enumerator.MoveNextAsync().AsTask());
        Assert.Equal(RemoteControlRuntimeErrorCode.FailedPrecondition, exception.ErrorCode);
    }

    [Fact]
    public async Task FrameStreamEmitsFramesWhenEnabled()
    {
        var root = new Border();
        var service = new RemoteControlFrameStreamService(
            new StaticRemoteControlRootProvider(root),
            new StubFrameProvider(),
            Options.Create(new AvaloniaRemoteControlOptions
            {
                AllowRemoteFrames = true,
                FrameStreamInterval = TimeSpan.FromMilliseconds(1),
            }),
            NullLogger<RemoteControlFrameStreamService>.Instance);
        using var cancellation = new CancellationTokenSource();

        await using var enumerator = service.WatchFramesAsync(cancellation.Token).GetAsyncEnumerator();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(1UL, enumerator.Current.Sequence);
        Assert.Equal(3, enumerator.Current.Png.Length);
        cancellation.Cancel();
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task InputDispatcherRejectsUnlessActionsAndInputAreEnabled()
    {
        var dispatcher = CreateInputDispatcher(new Border(), new AvaloniaRemoteControlOptions
        {
            AllowRemoteActions = true,
            AllowRemoteInput = false,
        });

        var result = await dispatcher.SendInputAsync(
            [new RemoteInputEvent { Kind = RemoteInputKind.PointerMove, X = 1, Y = 1 }],
            "desktop-client");

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task InputDispatcherRaisesPointerEventsWhenEnabled()
    {
        Point? pressedPoint = null;
        var root = new Border();
        root.Measure(new Size(100, 50));
        root.Arrange(new Rect(0, 0, 100, 50));
        root.AddHandler(
            InputElement.PointerPressedEvent,
            (_, args) => pressedPoint = args.GetPosition(root));
        var dispatcher = CreateInputDispatcher(root, new AvaloniaRemoteControlOptions
        {
            AllowRemoteActions = true,
            AllowRemoteInput = true,
        });

        var result = await dispatcher.SendInputAsync(
            [new RemoteInputEvent { Kind = RemoteInputKind.PointerPress, Button = RemoteMouseButton.Left, X = 20, Y = 10 }],
            "desktop-client");

        Assert.True(result.Succeeded);
        Assert.Equal(new Point(20, 10), pressedPoint);
    }

    [Fact]
    public async Task InputDispatcherDoesNotAuditTypedText()
    {
        var logger = new CapturingLogger<RemoteControlInputDispatcher>();
        var dispatcher = new RemoteControlInputDispatcher(
            new StaticRemoteControlRootProvider(new TextBox()),
            Options.Create(new AvaloniaRemoteControlOptions
            {
                AllowRemoteActions = true,
                AllowRemoteInput = true,
            }),
            new InlineRemoteControlDispatcher(),
            logger);

        await dispatcher.SendInputAsync(
            [new RemoteInputEvent { Kind = RemoteInputKind.Text, Text = "super-secret" }],
            "desktop-client");

        Assert.DoesNotContain(logger.Messages, message => message.Contains("super-secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SnapshotCaptureUsesContainingTopLevelWhenProviderReturnsChildRoot()
    {
        using var session = HeadlessUnitTestSession.StartNew(typeof(HeadlessAvaloniaTestApp));

        await session.Dispatch(async () =>
        {
            var childRoot = new Border { Name = "ChildRoot" };
            var window = new Window
            {
                Content = childRoot,
                Width = 300,
                Height = 200,
            };
            window.Show();
            var provider = new AvaloniaControlTreeSnapshotProvider(
                Options.Create(new AvaloniaRemoteControlOptions()),
                new InlineRemoteControlDispatcher());

            var snapshot = await provider.CaptureSnapshotAsync(childRoot);

            Assert.Equal(nameof(Window), snapshot.Nodes[0].TypeName);
            Assert.Contains(
                snapshot.Nodes,
                node => node.Name == "ChildRoot" && node.ParentId is not null);
            window.Close();

            return true;
        }, CancellationToken.None);
    }

    private static RemoteControlInputDispatcher CreateInputDispatcher(Control root, AvaloniaRemoteControlOptions options)
    {
        return new RemoteControlInputDispatcher(
            new StaticRemoteControlRootProvider(root),
            Options.Create(options),
            new InlineRemoteControlDispatcher(),
            NullLogger<RemoteControlInputDispatcher>.Instance);
    }

    private sealed class StubFrameProvider : IRemoteControlFrameProvider
    {
        public ValueTask<RemoteControlFrame> CaptureFrameAsync(
            Control root,
            ulong sequence,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new RemoteControlFrame(
                sequence,
                [1, 2, 3],
                10,
                10,
                10,
                10,
                1,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    private sealed class HeadlessAvaloniaTestApp : Application
    {
        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<HeadlessAvaloniaTestApp>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions());
        }
    }

}
