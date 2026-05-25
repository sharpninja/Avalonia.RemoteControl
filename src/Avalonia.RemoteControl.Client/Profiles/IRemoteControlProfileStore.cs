namespace Avalonia.RemoteControl.Client.Profiles;

/// <summary>
/// Stores user-scoped remote-control connection profiles.
/// </summary>
public interface IRemoteControlProfileStore
{
    /// <summary>
    /// Loads the default profile if one exists.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The saved profile, or <see langword="null" />.</returns>
    Task<RemoteControlConnectionProfile?> LoadDefaultAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the default profile.
    /// </summary>
    /// <param name="profile">The profile to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the save.</returns>
    Task SaveDefaultAsync(
        RemoteControlConnectionProfile profile,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Forgets the default profile.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing removal.</returns>
    Task ForgetDefaultAsync(CancellationToken cancellationToken = default);
}
