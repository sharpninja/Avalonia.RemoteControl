using Avalonia.RemoteControl.Client.Logging;
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

        Assert.Equal(LogLevel.Information, RemoteLogVerbosity.Default.MinimumLevel);
        Assert.Equal(
            ["Debug", "Information", "Warning", "Error"],
            RemoteLogVerbosity.Supported.Select(option => option.MinimumLevelName).ToArray());
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
}
