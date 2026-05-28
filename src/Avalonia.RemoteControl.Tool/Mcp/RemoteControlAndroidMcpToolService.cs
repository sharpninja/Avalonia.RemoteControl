using System.Text.Json;
using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Android;
using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Implements Android device and emulator MCP tools.
/// </summary>
public sealed class RemoteControlAndroidMcpToolService : IAndroidMcpToolService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AndroidDeviceManagerClient androidClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlAndroidMcpToolService"/> class.
    /// </summary>
    public RemoteControlAndroidMcpToolService()
        : this(new AndroidDeviceManagerClient(new ProcessAdbCommandRunner()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlAndroidMcpToolService"/> class.
    /// </summary>
    /// <param name="androidClient">Android device manager client.</param>
    public RemoteControlAndroidMcpToolService(AndroidDeviceManagerClient androidClient)
    {
        this.androidClient = androidClient ?? throw new ArgumentNullException(nameof(androidClient));
    }

    /// <inheritdoc />
    public async Task<string> CallAsync(
        string toolName,
        JsonElement arguments,
        CancellationToken cancellationToken = default)
    {
        return toolName switch
        {
            RemoteControlMcpToolCatalog.AndroidListDevices =>
                Serialize(await ListDevicesAsync(cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidListAvds =>
                Serialize(await ListAvdsAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidStartAvd =>
                Serialize(await StartAvdAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidInstallApk =>
                Serialize(await InstallApkAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidLaunchPackage =>
                Serialize(await LaunchPackageAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidForward =>
                Serialize(await ForwardAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidRemoveForward =>
                Serialize(await RemoveForwardAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidLogcat =>
                Serialize(await LogcatAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidScreenshot =>
                Serialize(await ScreenshotAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidUiTree =>
                Serialize(await UiTreeAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidTap =>
                Serialize(await TapAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidSwipe =>
                Serialize(await SwipeAsync(arguments, cancellationToken).ConfigureAwait(false)),
            RemoteControlMcpToolCatalog.AndroidText =>
                Serialize(await TextAsync(arguments, cancellationToken).ConfigureAwait(false)),
            _ => throw new ArgumentException($"Unknown Android tool: {toolName}"),
        };
    }

    private async Task<object> ListDevicesAsync(CancellationToken cancellationToken)
    {
        var devices = await androidClient.ListDevicesAsync(cancellationToken).ConfigureAwait(false);
        return new
        {
            devices = devices.Select(device => new
            {
                device.Serial,
                device.State,
                device.Model,
                device.Device,
            }),
        };
    }

    private async Task<object> ListAvdsAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var sdkPath = GetOptionalString(arguments, "androidSdkPath");
        var avds = await androidClient.ListAvdsAsync(sdkPath, cancellationToken).ConfigureAwait(false);
        return new
        {
            avds,
            emulatorPath = AndroidSdkLocator.ResolveEmulatorPath(sdkPath),
        };
    }

    private async Task<object> StartAvdAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "name");
        var sdkPath = GetOptionalString(arguments, "androidSdkPath");
        var additionalArguments = GetOptionalStringArray(arguments, "additionalArgs");
        var started = await androidClient.StartAvdAsync(
            name,
            sdkPath,
            additionalArguments,
            cancellationToken).ConfigureAwait(false);

        return new
        {
            started = true,
            name,
            processId = started.ProcessId,
            fileName = started.FileName,
            arguments = started.Arguments,
        };
    }

    private async Task<object> InstallApkAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var apkPath = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "apkPath");
        var replace = GetOptionalBoolean(arguments, "replace") ?? true;

        await androidClient.InstallApkAsync(serial, apkPath, replace, cancellationToken).ConfigureAwait(false);

        return new
        {
            installed = true,
            serial,
            apkPath,
            replace,
        };
    }

    private async Task<object> LaunchPackageAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var packageName = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "packageName");

        await androidClient.LaunchPackageAsync(serial, packageName, cancellationToken).ConfigureAwait(false);

        return new
        {
            launched = true,
            serial,
            packageName,
        };
    }

    private async Task<object> ForwardAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var hostPort = GetRequiredInt(arguments, "hostPort");
        var devicePort = GetRequiredInt(arguments, "devicePort");
        var forward = await androidClient.ForwardAsync(serial, hostPort, devicePort, cancellationToken)
            .ConfigureAwait(false);

        return new
        {
            forwarded = true,
            forward.Serial,
            forward.HostPort,
            forward.DevicePort,
            endpoint = forward.Endpoint.ToString(),
            transportProtocol = RemoteControlProtocol.AndroidBridgeTransportProtocol,
        };
    }

    private async Task<object> RemoveForwardAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var hostPort = GetRequiredInt(arguments, "hostPort");

        await androidClient.RemoveForwardAsync(serial, hostPort, cancellationToken).ConfigureAwait(false);

        return new
        {
            removed = true,
            serial,
            hostPort,
        };
    }

    private async Task<object> LogcatAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var packageName = GetOptionalString(arguments, "packageName");
        var pid = GetOptionalInt(arguments, "pid");
        var lines = GetOptionalInt(arguments, "lines") ?? 200;
        var resolvedPid = pid ?? (packageName is null
            ? null
            : await androidClient.ResolvePackagePidAsync(serial, packageName, cancellationToken).ConfigureAwait(false));
        var output = await androidClient.ReadLogcatAsync(
            serial,
            packageName,
            resolvedPid,
            lines,
            cancellationToken).ConfigureAwait(false);

        return new
        {
            serial,
            packageName,
            pid = resolvedPid,
            lines,
            output,
        };
    }

    private async Task<object> ScreenshotAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var outputPath = GetOptionalString(arguments, "outputPath");
        var path = await androidClient.CaptureScreenshotAsync(serial, outputPath, cancellationToken)
            .ConfigureAwait(false);
        var length = File.Exists(path) ? new FileInfo(path).Length : 0;

        return new
        {
            captured = true,
            serial,
            outputPath = path,
            bytes = length,
        };
    }

    private async Task<object> UiTreeAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var outputPath = GetOptionalString(arguments, "outputPath");
        var dump = await androidClient.DumpUiTreeAsync(serial, outputPath, cancellationToken)
            .ConfigureAwait(false);

        return new
        {
            serial,
            outputPath = dump.OutputPath,
            xml = dump.Xml,
        };
    }

    private async Task<object> TapAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var x = GetRequiredInt(arguments, "x");
        var y = GetRequiredInt(arguments, "y");

        await androidClient.TapAsync(serial, x, y, cancellationToken).ConfigureAwait(false);

        return new
        {
            sent = true,
            serial,
            x,
            y,
        };
    }

    private async Task<object> SwipeAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var startX = GetRequiredInt(arguments, "startX");
        var startY = GetRequiredInt(arguments, "startY");
        var endX = GetRequiredInt(arguments, "endX");
        var endY = GetRequiredInt(arguments, "endY");
        var durationMilliseconds = GetOptionalInt(arguments, "durationMilliseconds") ?? 300;

        await androidClient.SwipeAsync(
            serial,
            startX,
            startY,
            endX,
            endY,
            durationMilliseconds,
            cancellationToken).ConfigureAwait(false);

        return new
        {
            sent = true,
            serial,
            startX,
            startY,
            endX,
            endY,
            durationMilliseconds,
        };
    }

    private async Task<object> TextAsync(JsonElement arguments, CancellationToken cancellationToken)
    {
        var serial = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "serial");
        var text = RemoteControlMcpToolCatalog.GetRequiredString(arguments, "text");

        await androidClient.TextAsync(serial, text, cancellationToken).ConfigureAwait(false);

        return new
        {
            sent = true,
            serial,
            characterCount = text.Length,
        };
    }

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string? GetOptionalString(JsonElement arguments, string propertyName)
    {
        return arguments.TryGetProperty(propertyName, out var value)
            && value.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()
                : null;
    }

    private static IReadOnlyList<string> GetOptionalStringArray(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value)
            || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    private static int GetRequiredInt(JsonElement arguments, string propertyName)
    {
        return GetOptionalInt(arguments, propertyName)
            ?? throw new ArgumentException($"Argument '{propertyName}' is required.");
    }

    private static int? GetOptionalInt(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var intValue))
        {
            return intValue;
        }

        if (value.ValueKind == JsonValueKind.String
            && int.TryParse(value.GetString(), out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException($"Argument '{propertyName}' must be an integer.");
    }

    private static bool? GetOptionalBoolean(JsonElement arguments, string propertyName)
    {
        if (!arguments.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            _ => throw new ArgumentException($"Argument '{propertyName}' must be a boolean."),
        };
    }
}
