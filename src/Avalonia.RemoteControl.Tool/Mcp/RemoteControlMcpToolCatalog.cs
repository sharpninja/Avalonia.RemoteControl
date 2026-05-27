using System.Text.Json;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Defines MCP tools exposed by the desktop remote-control tool.
/// </summary>
public static class RemoteControlMcpToolCatalog
{
    /// <summary>
    /// Gets the Codex MCP server configuration name used by the embedded terminal preset.
    /// </summary>
    public const string CodexServerConfigName = "avalonia_remote_control";

    /// <summary>
    /// Gets the MCP server implementation name.
    /// </summary>
    public const string ServerName = "avalonia-remote-control";

    /// <summary>
    /// Gets the MCP server display title.
    /// </summary>
    public const string ServerTitle = "Avalonia Remote Control";

    /// <summary>
    /// Gets the capabilities tool name.
    /// </summary>
    public const string GetCapabilities = "avalonia_remote_get_capabilities";

    /// <summary>
    /// Gets the snapshot tool name.
    /// </summary>
    public const string GetSnapshot = "avalonia_remote_get_snapshot";

    /// <summary>
    /// Gets the click tool name.
    /// </summary>
    public const string InvokeClick = "avalonia_remote_invoke_click";

    /// <summary>
    /// Gets the focus tool name.
    /// </summary>
    public const string Focus = "avalonia_remote_focus";

    /// <summary>
    /// Gets the property mutation tool name.
    /// </summary>
    public const string SetProperty = "avalonia_remote_set_property";

    /// <summary>
    /// Creates the prompt seeded into embedded Codex sessions.
    /// </summary>
    /// <returns>Prompt text that explains how to use the MCP tools.</returns>
    public static string CreateCodexSeedPrompt() =>
        "You are connected to Avalonia Remote Control through MCP server `"
        + CodexServerConfigName
        + "`. Use `"
        + GetCapabilities
        + "` first to understand the connected debug target. Use `"
        + GetSnapshot
        + "` to inspect the current Avalonia control tree and find node IDs by type, name, automation ID, classes, bounds, visibility, enabled state, focus state, and public properties. "
        + "Do not use screenshots or pixel inspection as the primary way to choose controls; screenshots are only for visual confirmation when the tree is ambiguous. "
        + "To interact, refresh the tree, choose a current node ID, call `"
        + Focus
        + "` when focus matters, call `"
        + InvokeClick
        + "` for click actions, and call `"
        + SetProperty
        + "` only for approved public properties. After any mutation, call `"
        + GetSnapshot
        + "` again to verify the result. If a node ID is stale or missing, refresh the snapshot and locate the control again instead of guessing.";

    /// <summary>
    /// Creates MCP initialize instructions.
    /// </summary>
    /// <returns>Instructions returned during the MCP initialize handshake.</returns>
    public static string CreateInitializeInstructions() =>
        "Use these tools only against debug targets the user has explicitly configured. "
        + CreateCodexSeedPrompt();

    /// <summary>
    /// Creates MCP tool definitions.
    /// </summary>
    /// <returns>Tool definition objects.</returns>
    public static IReadOnlyList<object> CreateDefinitions() =>
    [
        CreateTool(
            GetCapabilities,
            "Get Remote Capabilities",
            "Gets the capabilities exposed by the connected Avalonia remote-control endpoint. Call this before other tools.",
            CreateEmptySchema()),
        CreateTool(
            GetSnapshot,
            "Get Remote Tree Snapshot",
            "Gets the current Avalonia control-tree snapshot. Use this tree data to find controls and node IDs instead of relying on screenshots.",
            CreateEmptySchema()),
        CreateTool(
            InvokeClick,
            "Invoke Remote Click",
            "Invokes a click on an approved current node ID found from the latest control-tree snapshot.",
            CreateNodeSchema()),
        CreateTool(
            Focus,
            "Focus Remote Node",
            "Requests focus on an approved current node ID found from the latest control-tree snapshot.",
            CreateNodeSchema()),
        CreateTool(
            SetProperty,
            "Set Remote Property",
            "Sets an approved public property on a current node ID, then callers should refresh the snapshot to verify the result.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["nodeId"] = StringProperty("Stable remote-control node ID."),
                    ["propertyName"] = StringProperty("Public property name to set."),
                    ["value"] = StringProperty("String representation of the target value."),
                },
                required = new[] { "nodeId", "propertyName", "value" },
                additionalProperties = false,
            }),
    ];

    internal static string GetRequiredString(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new ArgumentException($"Argument '{propertyName}' is required.");
        }

        return value.GetString()!;
    }

    private static object CreateTool(string name, string title, string description, object inputSchema) =>
        new
        {
            name,
            title,
            description,
            inputSchema,
        };

    private static object CreateEmptySchema() =>
        new
        {
            type = "object",
            properties = new Dictionary<string, object>(),
            additionalProperties = false,
        };

    private static object CreateNodeSchema() =>
        new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["nodeId"] = StringProperty("Stable remote-control node ID."),
            },
            required = new[] { "nodeId" },
            additionalProperties = false,
        };

    private static object StringProperty(string description) =>
        new
        {
            type = "string",
            description,
        };
}
