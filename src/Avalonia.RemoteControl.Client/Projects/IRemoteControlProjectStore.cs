namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Persists user-scoped remote-control project documents.
/// </summary>
public interface IRemoteControlProjectStore
{
    /// <summary>
    /// Loads a project document by identifier.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The project document, or null when it does not exist.</returns>
    Task<RemoteControlProjectDocument?> LoadAsync(
        string projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a project document.
    /// </summary>
    /// <param name="document">Project document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the document is saved.</returns>
    Task SaveAsync(
        RemoteControlProjectDocument document,
        CancellationToken cancellationToken = default);
}
