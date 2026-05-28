using System.Text.Json;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Handles MCP tool calls for Android device and emulator operations.
/// </summary>
public interface IAndroidMcpToolService
{
    /// <summary>
    /// Invokes an Android MCP tool.
    /// </summary>
    /// <param name="toolName">MCP tool name.</param>
    /// <param name="arguments">Tool arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON payload returned to MCP clients.</returns>
    Task<string> CallAsync(
        string toolName,
        JsonElement arguments,
        CancellationToken cancellationToken = default);
}
