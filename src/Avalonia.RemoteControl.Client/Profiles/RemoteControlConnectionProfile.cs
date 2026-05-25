namespace Avalonia.RemoteControl.Client.Profiles;

/// <summary>
/// Represents a saved remote-control connection profile.
/// </summary>
public sealed record RemoteControlConnectionProfile
{
    /// <summary>
    /// Gets or sets the endpoint URI text.
    /// </summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the bearer token.
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the profile update timestamp.
    /// </summary>
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
