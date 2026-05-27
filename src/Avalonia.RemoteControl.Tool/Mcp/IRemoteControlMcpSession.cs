namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// JSON-oriented adapter used by the tool-side MCP server to invoke remote-control operations.
/// </summary>
public interface IRemoteControlMcpSession : IDisposable
{
    /// <summary>
    /// Gets endpoint capabilities as JSON.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON payload.</returns>
    Task<string> GetCapabilitiesJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tree snapshot as JSON.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON payload.</returns>
    Task<string> GetSnapshotJsonAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a click and returns the command result as JSON.
    /// </summary>
    /// <param name="nodeId">Remote node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON payload.</returns>
    Task<string> InvokeClickJsonAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests focus and returns the command result as JSON.
    /// </summary>
    /// <param name="nodeId">Remote node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON payload.</returns>
    Task<string> FocusJsonAsync(string nodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a property and returns the command result as JSON.
    /// </summary>
    /// <param name="nodeId">Remote node ID.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="value">String value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON payload.</returns>
    Task<string> SetPropertyJsonAsync(
        string nodeId,
        string propertyName,
        string value,
        CancellationToken cancellationToken = default);
}
