using System.Text.Json;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Provides ADB device discovery and port-forward lifecycle operations.
/// </summary>
public sealed class AdbClient
{
    private const string MarkerPath = "files/avalonia-remote-control.json";
    private readonly IAdbCommandRunner runner;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdbClient"/> class.
    /// </summary>
    /// <param name="runner">The adb command runner.</param>
    public AdbClient(IAdbCommandRunner runner)
    {
        this.runner = runner;
    }

    /// <summary>
    /// Lists connected ADB devices and emulators.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Parsed ADB devices.</returns>
    public async Task<IReadOnlyList<AdbDevice>> ListDevicesAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await runner.RunAsync(["devices", "-l"], cancellationToken).ConfigureAwait(false);
        EnsureSuccess(result, "adb devices failed.");
        return AdbDeviceParser.Parse(result.StandardOutput);
    }

    /// <summary>
    /// Reads the package-private remote-control marker through adb run-as.
    /// </summary>
    /// <param name="serial">The adb serial.</param>
    /// <param name="packageName">The debuggable package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovered endpoint metadata.</returns>
    public async Task<AdbEndpointInfo> DiscoverEndpointAsync(
        string serial,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var result = await runner.RunAsync(
            ["-s", serial, "shell", "run-as", packageName, "cat", MarkerPath],
            cancellationToken).ConfigureAwait(false);

        EnsureSuccess(result, "Unable to read Avalonia.RemoteControl Android marker.");

        try
        {
            using var document = JsonDocument.Parse(result.StandardOutput);
            var root = document.RootElement;
            var devicePort = TryGetInt32(root, "devicePort")
                ?? TryGetInt32(root, "port")
                ?? throw new FormatException("Marker did not contain a devicePort.");
            var token = TryGetString(root, "token");
            var protocol = TryGetString(root, "protocol")
                ?? TryGetString(root, "transport")
                ?? TryGetString(root, "bridgeProtocol")
                ?? AdbEndpointInfo.GrpcProtocol;
            var protocolVersion = TryGetString(root, "protocolVersion")
                ?? TryGetString(root, "schemaVersion");

            return new AdbEndpointInfo(devicePort, token, protocol, protocolVersion);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Avalonia.RemoteControl Android marker is not valid JSON.",
                ex);
        }
    }

    /// <summary>
    /// Checks whether the selected Android package currently has a running process.
    /// </summary>
    /// <param name="serial">The adb serial.</param>
    /// <param name="packageName">The Android package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when adb reports a process ID; otherwise <see langword="false"/>.</returns>
    public async Task<bool> IsPackageRunningAsync(
        string serial,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var result = await runner.RunAsync(
            ["-s", serial, "shell", "pidof", packageName],
            cancellationToken).ConfigureAwait(false);

        return result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput);
    }

    /// <summary>
    /// Launches the selected Android package by asking adb monkey to inject a launcher event.
    /// </summary>
    /// <param name="serial">The adb serial.</param>
    /// <param name="packageName">The Android package name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the launch request.</returns>
    public async Task LaunchPackageAsync(
        string serial,
        string packageName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var result = await runner.RunAsync(
            ["-s", serial, "shell", "monkey", "-p", packageName, "1"],
            cancellationToken).ConfigureAwait(false);

        EnsureSuccess(result, $"Unable to launch Android package '{packageName}'.");
    }

    /// <summary>
    /// Waits until adb reports that the selected package has a running process.
    /// </summary>
    /// <param name="serial">The adb serial.</param>
    /// <param name="packageName">The Android package name.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="pollInterval">Polling interval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true" /> when the package starts; otherwise <see langword="false" />.</returns>
    public async Task<bool> WaitForPackageRunningAsync(
        string serial,
        string packageName,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ArgumentException.ThrowIfNullOrWhiteSpace(packageName);

        var stopAt = DateTimeOffset.UtcNow + timeout;
        var delay = pollInterval <= TimeSpan.Zero
            ? TimeSpan.FromMilliseconds(1)
            : pollInterval;

        do
        {
            if (await IsPackageRunningAsync(serial, packageName, cancellationToken).ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        while (DateTimeOffset.UtcNow < stopAt);

        return false;
    }

    /// <summary>
    /// Creates an adb forward from a host port to an Android-side port.
    /// </summary>
    /// <param name="serial">The adb serial.</param>
    /// <param name="hostPort">The host-side port.</param>
    /// <param name="devicePort">The Android-side port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created forward metadata.</returns>
    public async Task<AdbForward> ForwardAsync(
        string serial,
        int hostPort,
        int devicePort,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ValidatePort(hostPort, nameof(hostPort));
        ValidatePort(devicePort, nameof(devicePort));

        var result = await runner.RunAsync(
            ["-s", serial, "forward", $"tcp:{hostPort}", $"tcp:{devicePort}"],
            cancellationToken).ConfigureAwait(false);

        EnsureSuccess(result, "adb forward failed.");

        return new AdbForward(
            serial,
            hostPort,
            devicePort,
            new Uri($"http://127.0.0.1:{hostPort}"));
    }

    /// <summary>
    /// Removes an adb forward from the selected device.
    /// </summary>
    /// <param name="serial">The adb serial.</param>
    /// <param name="hostPort">The host-side forwarded port.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing cleanup.</returns>
    public async Task RemoveForwardAsync(
        string serial,
        int hostPort,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serial);
        ValidatePort(hostPort, nameof(hostPort));

        var result = await runner.RunAsync(
            ["-s", serial, "forward", "--remove", $"tcp:{hostPort}"],
            cancellationToken).ConfigureAwait(false);

        EnsureSuccess(result, "adb forward cleanup failed.");
    }

    private static void EnsureSuccess(AdbCommandResult result, string message)
    {
        if (result.ExitCode == 0)
        {
            return;
        }

        throw new AdbCommandFailedException(message, result);
    }

    private static void ValidatePort(int port, string parameterName)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Port must be between 1 and 65535.");
        }
    }

    private static int? TryGetInt32(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static string? TryGetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) ? property.GetString() : null;
    }
}
