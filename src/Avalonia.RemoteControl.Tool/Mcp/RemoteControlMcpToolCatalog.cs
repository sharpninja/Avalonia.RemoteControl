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
    /// Gets the Android device-listing tool name.
    /// </summary>
    public const string AndroidListDevices = "avalonia_android_list_devices";

    /// <summary>
    /// Gets the Android virtual-device listing tool name.
    /// </summary>
    public const string AndroidListAvds = "avalonia_android_list_avds";

    /// <summary>
    /// Gets the Android virtual-device launch tool name.
    /// </summary>
    public const string AndroidStartAvd = "avalonia_android_start_avd";

    /// <summary>
    /// Gets the Android APK install tool name.
    /// </summary>
    public const string AndroidInstallApk = "avalonia_android_install_apk";

    /// <summary>
    /// Gets the Android package launch tool name.
    /// </summary>
    public const string AndroidLaunchPackage = "avalonia_android_launch_package";

    /// <summary>
    /// Gets the Android ADB forward tool name.
    /// </summary>
    public const string AndroidForward = "avalonia_android_forward";

    /// <summary>
    /// Gets the Android ADB forward removal tool name.
    /// </summary>
    public const string AndroidRemoveForward = "avalonia_android_remove_forward";

    /// <summary>
    /// Gets the Android logcat tool name.
    /// </summary>
    public const string AndroidLogcat = "avalonia_android_logcat";

    /// <summary>
    /// Gets the Android screenshot tool name.
    /// </summary>
    public const string AndroidScreenshot = "avalonia_android_screenshot";

    /// <summary>
    /// Gets the Android UIAutomator tree dump tool name.
    /// </summary>
    public const string AndroidUiTree = "avalonia_android_ui_tree";

    /// <summary>
    /// Gets the Android tap input tool name.
    /// </summary>
    public const string AndroidTap = "avalonia_android_tap";

    /// <summary>
    /// Gets the Android swipe input tool name.
    /// </summary>
    public const string AndroidSwipe = "avalonia_android_swipe";

    /// <summary>
    /// Gets the Android text input tool name.
    /// </summary>
    public const string AndroidText = "avalonia_android_text";

    private static readonly HashSet<string> AndroidToolNames =
    [
        AndroidListDevices,
        AndroidListAvds,
        AndroidStartAvd,
        AndroidInstallApk,
        AndroidLaunchPackage,
        AndroidForward,
        AndroidRemoveForward,
        AndroidLogcat,
        AndroidScreenshot,
        AndroidUiTree,
        AndroidTap,
        AndroidSwipe,
        AndroidText,
    ];

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
        + "` only for approved public properties. Use Android tools such as `"
        + AndroidListDevices
        + "`, `"
        + AndroidListAvds
        + "`, `"
        + AndroidStartAvd
        + "`, `"
        + AndroidInstallApk
        + "`, `"
        + AndroidLaunchPackage
        + "`, `"
        + AndroidForward
        + "`, `"
        + AndroidLogcat
        + "`, `"
        + AndroidUiTree
        + "`, and `"
        + AndroidScreenshot
        + "` to prepare emulators, devices, app launches, diagnostics, and OS-level checks without a separate Android MCP server. "
        + "Use `"
        + AndroidTap
        + "`, `"
        + AndroidSwipe
        + "`, and `"
        + AndroidText
        + "` for Android shell-level input only when the Avalonia tree cannot address the target. After any mutation, call `"
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
        CreateTool(
            AndroidListDevices,
            "List Android Devices",
            "Lists connected Android devices and emulators through adb devices -l.",
            CreateEmptySchema()),
        CreateTool(
            AndroidListAvds,
            "List Android Virtual Devices",
            "Lists configured Android virtual devices through the Android SDK emulator command.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["androidSdkPath"] = StringProperty("Optional Android SDK root path."),
                },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidStartAvd,
            "Start Android Virtual Device",
            "Starts a named Android virtual device such as Pixel_6 through the Android SDK emulator command.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["name"] = StringProperty("AVD name from avalonia_android_list_avds."),
                    ["androidSdkPath"] = StringProperty("Optional Android SDK root path."),
                    ["additionalArgs"] = StringArrayProperty("Optional emulator arguments, passed without shell expansion."),
                },
                required = new[] { "name" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidInstallApk,
            "Install Android APK",
            "Installs a local APK on a selected Android device or emulator.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["apkPath"] = StringProperty("Local APK path."),
                    ["replace"] = BooleanProperty("Whether adb install should replace an existing package. Defaults to true."),
                    ["noIncremental"] = BooleanProperty("Whether adb install should disable incremental install. Defaults to true to avoid startup ANR false positives on emulators."),
                },
                required = new[] { "serial", "apkPath" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidLaunchPackage,
            "Launch Android Package",
            "Launches an installed Android package using adb monkey.",
            CreateSerialPackageSchema()),
        CreateTool(
            AndroidForward,
            "Create Android Forward",
            "Creates an adb TCP forward from a host port to a device port.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["hostPort"] = IntegerProperty("Host-side TCP port."),
                    ["devicePort"] = IntegerProperty("Device-side TCP port."),
                },
                required = new[] { "serial", "hostPort", "devicePort" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidRemoveForward,
            "Remove Android Forward",
            "Removes an adb TCP forward from the selected device.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["hostPort"] = IntegerProperty("Host-side TCP port."),
                },
                required = new[] { "serial", "hostPort" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidLogcat,
            "Read Android Logcat",
            "Reads bounded logcat output from a selected device, optionally filtered by package PID.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["packageName"] = StringProperty("Optional Android package name used to resolve a PID filter."),
                    ["pid"] = IntegerProperty("Optional process ID filter."),
                    ["lines"] = IntegerProperty("Maximum log lines to return. Defaults to 200."),
                },
                required = new[] { "serial" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidScreenshot,
            "Capture Android Screenshot",
            "Captures a device screenshot to a local PNG file and returns the file path.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["outputPath"] = StringProperty("Optional local PNG output path."),
                },
                required = new[] { "serial" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidUiTree,
            "Dump Android UI Tree",
            "Dumps the Android UIAutomator hierarchy XML for OS-level and emulator checks.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["outputPath"] = StringProperty("Optional local XML output path."),
                },
                required = new[] { "serial" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidTap,
            "Send Android Tap",
            "Sends a physical-pixel tap using adb shell input.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["x"] = IntegerProperty("Physical-pixel X coordinate."),
                    ["y"] = IntegerProperty("Physical-pixel Y coordinate."),
                },
                required = new[] { "serial", "x", "y" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidSwipe,
            "Send Android Swipe",
            "Sends a physical-pixel swipe using adb shell input.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["startX"] = IntegerProperty("Start physical-pixel X coordinate."),
                    ["startY"] = IntegerProperty("Start physical-pixel Y coordinate."),
                    ["endX"] = IntegerProperty("End physical-pixel X coordinate."),
                    ["endY"] = IntegerProperty("End physical-pixel Y coordinate."),
                    ["durationMilliseconds"] = IntegerProperty("Optional swipe duration. Defaults to 300."),
                },
                required = new[] { "serial", "startX", "startY", "endX", "endY" },
                additionalProperties = false,
            }),
        CreateTool(
            AndroidText,
            "Send Android Text",
            "Sends text input using adb shell input text. The result reports only character count.",
            new
            {
                type = "object",
                properties = new Dictionary<string, object>
                {
                    ["serial"] = StringProperty("ADB device serial."),
                    ["text"] = StringProperty("Text to send. Sensitive text should not be sent through this debug tool."),
                },
                required = new[] { "serial", "text" },
                additionalProperties = false,
            }),
    ];

    /// <summary>
    /// Determines whether a tool name belongs to the Android device-management catalog.
    /// </summary>
    /// <param name="toolName">Tool name.</param>
    /// <returns><see langword="true"/> when the tool is an Android tool.</returns>
    public static bool IsAndroidTool(string? toolName) =>
        toolName is not null && AndroidToolNames.Contains(toolName);

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

    private static object CreateSerialPackageSchema() =>
        new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["serial"] = StringProperty("ADB device serial."),
                ["packageName"] = StringProperty("Android package name."),
            },
            required = new[] { "serial", "packageName" },
            additionalProperties = false,
        };

    private static object StringProperty(string description) =>
        new
        {
            type = "string",
            description,
        };

    private static object StringArrayProperty(string description) =>
        new
        {
            type = "array",
            description,
            items = new
            {
                type = "string",
            },
        };

    private static object IntegerProperty(string description) =>
        new
        {
            type = "integer",
            description,
        };

    private static object BooleanProperty(string description) =>
        new
        {
            type = "boolean",
            description,
        };
}
