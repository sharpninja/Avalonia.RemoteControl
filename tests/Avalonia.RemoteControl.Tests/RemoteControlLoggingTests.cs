using System.Collections.Concurrent;
using Avalonia.RemoteControl.Client.Logging;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlLoggingTests
{
    [Fact]
    public void LogVerbosityOptionsExposeSupportedMinimumLevels()
    {
        Assert.Equal(
            ["Debug", "Information", "Warning", "Error"],
            RemoteLogVerbosity.Supported.Select(option => option.DisplayName).ToArray());

        Assert.Equal(LogLevel.Warning, RemoteLogVerbosity.Default.MinimumLevel);
        Assert.Equal(
            ["Debug", "Information", "Warning", "Error"],
            RemoteLogVerbosity.Supported.Select(option => option.MinimumLevelName).ToArray());
    }

    [Fact]
    public void LogDisplayFormatterIncludesDiagnosticMetadata()
    {
        var entry = new LogEntry
        {
            Sequence = 12,
            TimestampUtc = "2026-05-26T02:20:00.0000000Z",
            Level = "Warning",
            Category = "Sample.Category",
            EventId = 7,
            Message = "visible message",
            ExceptionSummary = "InvalidOperationException: failed",
            DroppedCount = 3,
            StructuredState = "Token=[redacted]",
            ScopeSummary = "RequestId=req-1",
        };

        var row = RemoteLogDisplayFormatter.Format(entry);

        Assert.Contains("#12", row, StringComparison.Ordinal);
        Assert.Contains("2026-05-26T02:20:00.0000000Z", row, StringComparison.Ordinal);
        Assert.Contains("Warning", row, StringComparison.Ordinal);
        Assert.Contains("Sample.Category", row, StringComparison.Ordinal);
        Assert.Contains("event=7", row, StringComparison.Ordinal);
        Assert.Contains("visible message", row, StringComparison.Ordinal);
        Assert.Contains("dropped=3", row, StringComparison.Ordinal);
        Assert.Contains("exception=InvalidOperationException: failed", row, StringComparison.Ordinal);
        Assert.Contains("state=Token=[redacted]", row, StringComparison.Ordinal);
        Assert.Contains("scope=RequestId=req-1", row, StringComparison.Ordinal);
    }

    [Fact]
    public void LogViewModelSharesRowsAndCountsOnlyRemoteEntries()
    {
        var viewModel = new RemoteLogViewModel();
        var propertyChanges = new List<string?>();
        viewModel.PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);

        var rows = viewModel.Rows;
        viewModel.SetStatus("Streaming logs (Debug); 1 entries.");
        rows.Add("client 2026-05-26T17:00:00Z: Log stream starting (Debug).");
        rows.Add("#1 2026-05-26T17:00:01Z Debug Sample: visible");

        Assert.Same(rows, viewModel.Rows);
        Assert.Equal(1, viewModel.RemoteEntryCount);
        Assert.Equal("Streaming logs (Debug); 1 entries.", viewModel.StatusText);
        Assert.Contains(nameof(RemoteLogViewModel.StatusText), propertyChanges);
    }

    [Fact]
    public void LogViewModelTracksPopOutOwnershipWithoutChangingRows()
    {
        var viewModel = new RemoteLogViewModel();
        var rows = viewModel.Rows;
        var propertyChanges = new List<string?>();
        viewModel.PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);
        rows.Add("#1 2026-05-26T17:00:01Z Debug Sample: visible");

        viewModel.PopOut();
        viewModel.Dock();

        Assert.False(viewModel.IsPoppedOut);
        Assert.Same(rows, viewModel.Rows);
        Assert.Equal(1, viewModel.RemoteEntryCount);
        Assert.Equal(
            2,
            propertyChanges.Count(name => string.Equals(name, nameof(RemoteLogViewModel.IsPoppedOut), StringComparison.Ordinal)));
    }

    [Fact]
    public void LogViewModelTracksSharedSelectedVerbosity()
    {
        var viewModel = new RemoteLogViewModel();
        var propertyChanges = new List<string?>();
        viewModel.PropertyChanged += (_, args) => propertyChanges.Add(args.PropertyName);

        var error = Assert.Single(viewModel.SupportedVerbosity, option => option.MinimumLevel == LogLevel.Error);
        viewModel.SelectedVerbosity = error;

        Assert.Same(RemoteLogVerbosity.Supported, viewModel.SupportedVerbosity);
        Assert.Equal(LogLevel.Warning, RemoteLogVerbosity.Default.MinimumLevel);
        Assert.Equal(error, viewModel.SelectedVerbosity);
        Assert.Contains(nameof(RemoteLogViewModel.SelectedVerbosity), propertyChanges);
    }

    [Fact]
    public void ServiceCollectionRegistersRemoteControlLoggerProvider()
    {
        var services = new ServiceCollection();

        services.AddAvaloniaRemoteControl();

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<RemoteControlLogStreamService>());
        Assert.Contains(
            provider.GetServices<ILoggerProvider>(),
            loggerProvider => loggerProvider is RemoteControlLoggerProvider);
    }

    [Fact]
    public async Task ServiceCollectionLetsRemoteProviderCaptureDebugWithoutLoweringOtherProviders()
    {
        var externalProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddProvider(externalProvider));
        services.AddAvaloniaRemoteControlRuntime();

        using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Sample.Debug");

        logger.LogDebug("debug visible to remote provider");
        logger.LogInformation("information visible to all providers");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var entry = await ReadFirstAsync(provider
            .GetRequiredService<RemoteControlLogBuffer>()
            .ReadAllAsync(LogLevel.Debug, "Sample", cts.Token));

        Assert.Equal(LogLevel.Debug, entry.Level);
        Assert.Contains("debug visible", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(externalProvider.Entries, captured => captured.Level == LogLevel.Debug);
        Assert.Contains(externalProvider.Entries, captured => captured.Level == LogLevel.Information);
    }

    [Fact]
    public async Task LoggerProviderRedactsSensitiveValues()
    {
        var options = Options.Create(new AvaloniaRemoteControlOptions());
        var buffer = new RemoteControlLogBuffer(options);
        using var provider = new RemoteControlLoggerProvider(buffer, options);
        var logger = provider.CreateLogger("Sample.Category");

        logger.LogInformation("Connecting with token={Token}", "abc123");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var entry = await ReadFirstAsync(buffer.ReadAllAsync(LogLevel.Information, "Sample", cts.Token));

        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("Sample.Category", entry.Category);
        Assert.Contains("[redacted]", entry.Message);
        Assert.DoesNotContain("abc123", entry.Message, StringComparison.Ordinal);
        Assert.Contains("Token=[redacted]", entry.StructuredState);
        Assert.DoesNotContain("abc123", entry.StructuredState, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LogBufferReportsDroppedEntriesWhenCapacityIsExceeded()
    {
        var options = Options.Create(new AvaloniaRemoteControlOptions
        {
            LogBufferCapacity = 1,
        });

        var buffer = new RemoteControlLogBuffer(options);

        buffer.Publish(new RemoteControlLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = LogLevel.Information,
            Category = "Test",
            Message = "first",
        });

        buffer.Publish(new RemoteControlLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = LogLevel.Information,
            Category = "Test",
            Message = "second",
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var entry = await ReadFirstAsync(buffer.ReadAllAsync(LogLevel.Trace, null, cts.Token));

        Assert.Equal("second", entry.Message);
        Assert.Equal(1UL, entry.DroppedCount);
    }

    [Fact]
    public async Task LogStreamFiltersByMinimumLevelAndCategoryPrefix()
    {
        var options = Options.Create(new AvaloniaRemoteControlOptions());
        var buffer = new RemoteControlLogBuffer(options);
        var stream = new RemoteControlLogStreamService(buffer);

        buffer.Publish(new RemoteControlLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = LogLevel.Debug,
            Category = "Sample.Ui",
            Message = "debug",
        });

        buffer.Publish(new RemoteControlLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = LogLevel.Warning,
            Category = "Other",
            Message = "other",
        });

        buffer.Publish(new RemoteControlLogEntry
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Level = LogLevel.Warning,
            Category = "Sample.Ui",
            Message = "warning",
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var entry = await ReadFirstAsync(stream.WatchEntriesAsync(LogLevel.Information, "Sample", cts.Token));

        Assert.Equal("warning", entry.Message);
        Assert.Equal("Sample.Ui", entry.Category);
    }

    private static async Task<RemoteControlLogEntry> ReadFirstAsync(
        IAsyncEnumerable<RemoteControlLogEntry> entries)
    {
        await foreach (var entry in entries)
        {
            return entry;
        }

        throw new InvalidOperationException("The log stream ended without an entry.");
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<CapturedLogEntry> entries = new();

        public IEnumerable<CapturedLogEntry> Entries => entries;

        public ILogger CreateLogger(string categoryName)
        {
            return new CapturingLogger(entries);
        }

        public void Dispose()
        {
        }
    }

    private sealed class CapturingLogger(ConcurrentQueue<CapturedLogEntry> entries) : ILogger
    {
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
            entries.Enqueue(new CapturedLogEntry(logLevel, formatter(state, exception)));
        }
    }

    private sealed record CapturedLogEntry(LogLevel Level, string Message);
}
