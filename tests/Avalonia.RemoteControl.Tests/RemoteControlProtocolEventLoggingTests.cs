using Avalonia.Controls;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Input;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Rendering;
using Avalonia.RemoteControl.Server.Runtime;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlProtocolEventLoggingTests
{
    [Fact]
    public async Task RuntimeWritesDebugMessagesForUnaryClientEvents()
    {
        var logger = new CapturingLogger<RemoteControlRuntime>();
        var runtime = CreateRuntime(
            new TextBlock { Name = "DebugRoot", Text = "Debug" },
            new AvaloniaRemoteControlOptions
            {
                AllowRemoteActions = true,
                AllowRemoteInput = true,
            },
            logger);

        runtime.GetCapabilities();
        await runtime.GetSnapshotAsync();
        await runtime.SendInputAsync([], "desktop-client");

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("received from client: GetCapabilities", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("sent to client: GetCapabilitiesResponse", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("received from client: GetSnapshot", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("sent to client: TreeSnapshot", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("received from client: SendInput; event count 0", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("sent to client: SendInputResult", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RuntimeWritesDebugMessagesForTreeAndFrameStreamEvents()
    {
        var logger = new CapturingLogger<RemoteControlRuntime>();
        var runtime = CreateRuntime(
            new TextBlock { Name = "StreamRoot", Text = "Stream" },
            new AvaloniaRemoteControlOptions
            {
                AllowRemoteFrames = true,
                TreeStreamInterval = TimeSpan.FromMinutes(1),
                FrameStreamInterval = TimeSpan.FromMinutes(1),
            },
            logger);

        await using (var tree = runtime.WatchSnapshotsAsync().GetAsyncEnumerator())
        {
            Assert.True(await tree.MoveNextAsync());
        }

        await using (var frames = runtime.WatchFramesAsync().GetAsyncEnumerator())
        {
            Assert.True(await frames.MoveNextAsync());
        }

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("received from client: WatchTree", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("sent to client: WatchTreeUpdate", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("WatchTree stream completed after 1 events", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("received from client: WatchFrames", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("sent to client: WatchFramesUpdate", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("WatchFrames stream completed after 1 events", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RuntimeLogsWatchLogsLifecycleWithoutEchoingEachLogEntry()
    {
        var logger = new CapturingLogger<RemoteControlRuntime>();
        var buffer = new RemoteControlLogBuffer(Options.Create(new AvaloniaRemoteControlOptions()));
        var runtime = CreateRuntime(
            new TextBlock { Name = "LogsRoot" },
            new AvaloniaRemoteControlOptions(),
            logger,
            buffer);

        buffer.Publish(new RemoteControlLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = LogLevel.Information,
            Category = "App",
            Message = "hello",
        });

        await using (var logs = runtime.WatchLogEntriesAsync(LogLevel.Debug, null).GetAsyncEnumerator())
        {
            Assert.True(await logs.MoveNextAsync());
        }

        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("received from client: WatchLogs", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("log stream opened for client", StringComparison.Ordinal));
        Assert.Contains(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("log stream completed for client after 1 entries", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry =>
            entry.Level == LogLevel.Debug &&
            entry.Message.Contains("LogEntry", StringComparison.Ordinal));
    }

    private static RemoteControlRuntime CreateRuntime(
        Control root,
        AvaloniaRemoteControlOptions options,
        CapturingLogger<RemoteControlRuntime> logger,
        RemoteControlLogBuffer? logBuffer = null)
    {
        var optionsAccessor = Options.Create(options);
        var rootProvider = new StaticRemoteControlRootProvider(root);
        var snapshotProvider = new AvaloniaControlTreeSnapshotProvider(
            optionsAccessor,
            new InlineRemoteControlDispatcher());
        var buffer = logBuffer ?? new RemoteControlLogBuffer(optionsAccessor);

        return new RemoteControlRuntime(
            new AvaloniaRemoteControlService(
                optionsAccessor,
                new CapturingLogger<AvaloniaRemoteControlService>()),
            snapshotProvider,
            rootProvider,
            new RemoteControlTreeStreamService(
                snapshotProvider,
                rootProvider,
                optionsAccessor),
            new RemoteControlLogStreamService(buffer),
            new RemoteControlFrameStreamService(
                rootProvider,
                new StubFrameProvider(),
                optionsAccessor,
                new CapturingLogger<RemoteControlFrameStreamService>()),
            new RemoteControlActionInvoker(
                snapshotProvider,
                optionsAccessor,
                new InlineRemoteControlDispatcher(),
                new CapturingLogger<RemoteControlActionInvoker>()),
            new RemoteControlPropertyMutationService(
                snapshotProvider,
                optionsAccessor,
                new InlineRemoteControlDispatcher(),
                new CapturingLogger<RemoteControlPropertyMutationService>()),
            new RemoteControlInputDispatcher(
                rootProvider,
                optionsAccessor,
                new InlineRemoteControlDispatcher(),
                new CapturingLogger<RemoteControlInputDispatcher>()),
            logger);
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
                1,
                1,
                1,
                1,
                1,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<CapturedLogEntry> Entries { get; } = [];

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
            Entries.Add(new CapturedLogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record CapturedLogEntry(LogLevel Level, string Message);
}
