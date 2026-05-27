using Microsoft.Extensions.Logging;

namespace Avalonia.RemoteControl.Client.Logging;

/// <summary>
/// Represents a supported remote log verbosity selection.
/// </summary>
/// <param name="DisplayName">Display name shown to users.</param>
/// <param name="MinimumLevel">Minimum log level requested from the remote log stream.</param>
public sealed record RemoteLogVerbosity(string DisplayName, LogLevel MinimumLevel)
{
    /// <summary>
    /// Gets the supported remote log verbosity options.
    /// </summary>
    public static IReadOnlyList<RemoteLogVerbosity> Supported { get; } =
    [
        new("Debug", LogLevel.Debug),
        new("Information", LogLevel.Information),
        new("Warning", LogLevel.Warning),
        new("Error", LogLevel.Error),
    ];

    /// <summary>
    /// Gets the default remote log verbosity.
    /// </summary>
    public static RemoteLogVerbosity Default { get; } = Supported[2];

    /// <summary>
    /// Gets the protocol minimum-level value sent to <c>WatchLogs</c>.
    /// </summary>
    public string MinimumLevelName => MinimumLevel.ToString();

    /// <inheritdoc />
    public override string ToString()
    {
        return DisplayName;
    }
}
