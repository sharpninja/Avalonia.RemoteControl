using System.Globalization;
using Avalonia.RemoteControl.Client.Adb;

namespace Avalonia.RemoteControl.Client.Android;

/// <summary>
/// Provides Android device, emulator, application, diagnostics, and input operations for tool integrations.
/// </summary>
public sealed class AndroidDeviceManagerClient
{
    private readonly IAdbCommandRunner adbRunner;
    private readonly IAndroidCommandRunner sdkRunner;
    private readonly AdbClient adbClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AndroidDeviceManagerClient"/> class.
    /// </summary>
    /// <param name="adbRunner">ADB command runner.</param>
    /// <param name="sdkRunner">Android SDK command runner.</param>
    public AndroidDeviceManagerClient(
        IAdbCommandRunner adbRunner,
        IAndroidCommandRunner? sdkRunner = null)
    {
        this.adbRunner = adbRunner ?? throw new ArgumentNullException(nameof(adbRunner));
        this.sdkRunner = sdkRunner ?? new ProcessAndroidCommandRunner();
        adbClient = new AdbClient(adbRunner);
    }

    /// <summary>
    /// Lists connected Android devices and emulators.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed adb devices.</returns>
    public Task<IReadOnlyList<AdbDevice>> ListDevicesAsync(CancellationToken cancellationToken = default) =>
        adbClient.ListDevicesAsync(cancellationToken);

    /// <summary>
    /// Lists configured Android virtual device names.
    /// </summary>
    /// <param name="androidSdkPath">Optional Android SDK root path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>AVD names.</returns>
    public async Task<IReadOnlyList<string>> ListAvdsAsync(
        string? androidSdkPath = null,
        CancellationToken cancellationToken = default)
    {
        var emulatorPath = AndroidSdkLocator.ResolveEmulatorPath(androidSdkPath);
        var result = await sdkRunner.RunAsync(
            emulatorPath,
            ["-list-avds"],
            cancellationToken).ConfigureAwait(false);

        EnsureSuccess(result, "Unable to list Android virtual devices.");

        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
    }

    /// <summary>
    /// Starts a configured Android virtual device.
    /// </summary>
    /// <param name="avdName">AVD name.</param>
    /// <param name="androidSdkPath">Optional Android SDK root path.</param>
    /// <param name="additionalArguments">Additional emulator arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Started emulator process metadata.</returns>
    public Task<AndroidStartedProcess> StartAvdAsync(
        string avdName,
        string? androidSdkPath = null,
        IReadOnlyList<string>? additionalArguments = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(avdName);

        var emulatorPath = AndroidSdkLocator.ResolveEmulatorPath(androidSdkPath);
        var arguments = new List<string> { "-avd", avdName };
        if (additionalArguments is not null)
        {
            arguments.AddRange(additionalArguments.Where(argument => !string.IsNullOrWhiteSpace(argument)));
        }

        return sdkRunner.StartAsync(emulatorPath, arguments, cancellationToken);
    }

    /// <summary>
    /// Installs an APK onto a selected device.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="apkPath">Local APK path.</param>
    /// <param name="replace">Whether to replace an existing package.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the install operation.</returns>
    public async Task InstallApkAsync(
        string serial,
        string apkPath,
        bool replace = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(apkPath);

        var arguments = new List<string> { "-s", serial, "install" };
        if (replace)
        {
            arguments.Add("-r");
        }

        arguments.Add(apkPath);
        var result = await adbRunner.RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, $"Unable to install APK '{apkPath}'.");
    }

    /// <summary>
    /// Launches the selected Android package.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="packageName">Android package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the launch request.</returns>
    public Task LaunchPackageAsync(
        string serial,
        string packageName,
        CancellationToken cancellationToken = default) =>
        adbClient.LaunchPackageAsync(serial, packageName, cancellationToken);

    /// <summary>
    /// Creates an adb TCP forward.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="hostPort">Host-side port.</param>
    /// <param name="devicePort">Device-side port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Forward metadata.</returns>
    public Task<AdbForward> ForwardAsync(
        string serial,
        int hostPort,
        int devicePort,
        CancellationToken cancellationToken = default) =>
        adbClient.ForwardAsync(serial, hostPort, devicePort, cancellationToken);

    /// <summary>
    /// Removes an adb TCP forward.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="hostPort">Host-side port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing cleanup.</returns>
    public Task RemoveForwardAsync(
        string serial,
        int hostPort,
        CancellationToken cancellationToken = default) =>
        adbClient.RemoveForwardAsync(serial, hostPort, cancellationToken);

    /// <summary>
    /// Reads logcat output from the selected device.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="packageName">Optional package name used to resolve a PID.</param>
    /// <param name="pid">Optional process ID filter.</param>
    /// <param name="lines">Number of lines to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Logcat text.</returns>
    public async Task<string> ReadLogcatAsync(
        string serial,
        string? packageName = null,
        int? pid = null,
        int lines = 200,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);

        if (pid is null && !string.IsNullOrWhiteSpace(packageName))
        {
            pid = await ResolvePackagePidAsync(serial, packageName, cancellationToken).ConfigureAwait(false);
        }

        var arguments = new List<string>
        {
            "-s",
            serial,
            "logcat",
            "-d",
        };

        if (pid is not null)
        {
            arguments.Add("--pid");
            arguments.Add(pid.Value.ToString(CultureInfo.InvariantCulture));
        }

        arguments.Add("-t");
        arguments.Add(Math.Max(1, lines).ToString(CultureInfo.InvariantCulture));

        var result = await adbRunner.RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "Unable to read Android logcat.");
        return result.StandardOutput;
    }

    /// <summary>
    /// Resolves the current process ID for an Android package.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="packageName">Android package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The process ID when the package is running; otherwise <see langword="null"/>.</returns>
    public async Task<int?> ResolvePackagePidAsync(
        string serial,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var result = await adbRunner.RunAsync(
            ["-s", serial, "shell", "pidof", packageName],
            cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return null;
        }

        var first = result.StandardOutput
            .Split([' ', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return int.TryParse(first, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// Captures a screenshot from the selected device to a local PNG file.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="outputPath">Optional local output path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Local PNG path.</returns>
    public async Task<string> CaptureScreenshotAsync(
        string serial,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);

        var localPath = PrepareOutputPath(serial, outputPath, "screenshot", ".png");
        var devicePath = $"/sdcard/Download/avalonia-remote-control-{Guid.NewGuid():N}.png";

        await RunAdbAsync(["-s", serial, "shell", "screencap", "-p", devicePath], "Unable to capture Android screenshot.", cancellationToken)
            .ConfigureAwait(false);
        await RunAdbAsync(["-s", serial, "pull", devicePath, localPath], "Unable to pull Android screenshot.", cancellationToken)
            .ConfigureAwait(false);
        await TryRunAdbAsync(["-s", serial, "shell", "rm", "-f", devicePath], cancellationToken).ConfigureAwait(false);

        return localPath;
    }

    /// <summary>
    /// Dumps the Android UIAutomator hierarchy from the selected device.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="outputPath">Optional local XML output path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Hierarchy XML and optional local path.</returns>
    public async Task<AndroidUiTreeDump> DumpUiTreeAsync(
        string serial,
        string? outputPath = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);

        var localPath = string.IsNullOrWhiteSpace(outputPath)
            ? null
            : PrepareOutputPath(serial, outputPath, "ui-tree", ".xml");
        var devicePath = $"/sdcard/Download/avalonia-remote-control-{Guid.NewGuid():N}.xml";

        await RunAdbAsync(["-s", serial, "shell", "uiautomator", "dump", devicePath], "Unable to dump Android UI tree.", cancellationToken)
            .ConfigureAwait(false);
        var xml = await RunAdbAsync(["-s", serial, "shell", "cat", devicePath], "Unable to read Android UI tree.", cancellationToken)
            .ConfigureAwait(false);
        await TryRunAdbAsync(["-s", serial, "shell", "rm", "-f", devicePath], cancellationToken).ConfigureAwait(false);

        if (localPath is not null)
        {
            await File.WriteAllTextAsync(localPath, xml, cancellationToken).ConfigureAwait(false);
        }

        return new AndroidUiTreeDump(xml, localPath);
    }

    /// <summary>
    /// Sends a tap input event through adb.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="x">X coordinate in physical pixels.</param>
    /// <param name="y">Y coordinate in physical pixels.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing input injection.</returns>
    public Task TapAsync(
        string serial,
        int x,
        int y,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        return RunAdbAsync(
            ["-s", serial, "shell", "input", "tap", Format(x), Format(y)],
            "Unable to send Android tap input.",
            cancellationToken);
    }

    /// <summary>
    /// Sends a swipe input event through adb.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="startX">Start X coordinate in physical pixels.</param>
    /// <param name="startY">Start Y coordinate in physical pixels.</param>
    /// <param name="endX">End X coordinate in physical pixels.</param>
    /// <param name="endY">End Y coordinate in physical pixels.</param>
    /// <param name="durationMilliseconds">Swipe duration in milliseconds.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing input injection.</returns>
    public Task SwipeAsync(
        string serial,
        int startX,
        int startY,
        int endX,
        int endY,
        int durationMilliseconds = 300,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        return RunAdbAsync(
            [
                "-s",
                serial,
                "shell",
                "input",
                "swipe",
                Format(startX),
                Format(startY),
                Format(endX),
                Format(endY),
                Format(Math.Max(1, durationMilliseconds)),
            ],
            "Unable to send Android swipe input.",
            cancellationToken);
    }

    /// <summary>
    /// Sends text input through adb.
    /// </summary>
    /// <param name="serial">ADB serial.</param>
    /// <param name="text">Text to enter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing input injection.</returns>
    public Task TextAsync(
        string serial,
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentNullException.ThrowIfNull(text);

        if (text.Any(static character => char.IsControl(character)))
        {
            throw new ArgumentException("Android input text cannot contain control characters.", nameof(text));
        }

        return RunAdbAsync(
            ["-s", serial, "shell", "input", "text", EscapeInputText(text)],
            "Unable to send Android text input.",
            cancellationToken);
    }

    private async Task<string> RunAdbAsync(
        IReadOnlyList<string> arguments,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        var result = await adbRunner.RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, errorMessage);
        return result.StandardOutput;
    }

    private async Task TryRunAdbAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            await adbRunner.RunAsync(arguments, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
        }
    }

    private static void EnsureSuccess(AdbCommandResult result, string message)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        throw new AdbCommandFailedException(message, result);
    }

    private static string PrepareOutputPath(
        string serial,
        string? outputPath,
        string stem,
        string extension)
    {
        var resolved = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(
                Path.GetTempPath(),
                "Avalonia.RemoteControl",
                "android",
                $"{SanitizeFileName(serial)}-{stem}-{DateTimeOffset.UtcNow:yyyyMMddTHHmmssZ}{extension}")
            : outputPath;

        var directory = Path.GetDirectoryName(resolved);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return resolved;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }

    private static string EscapeInputText(string value) =>
        value.Replace("%", "%%", StringComparison.Ordinal)
            .Replace(" ", "%s", StringComparison.Ordinal);

    private static string Format(int value) => value.ToString(CultureInfo.InvariantCulture);
}

/// <summary>
/// Represents an Android UIAutomator hierarchy dump.
/// </summary>
/// <param name="Xml">Hierarchy XML.</param>
/// <param name="OutputPath">Optional local XML output path.</param>
public sealed record AndroidUiTreeDump(string Xml, string? OutputPath);
