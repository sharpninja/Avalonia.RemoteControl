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

            return new AdbEndpointInfo(devicePort, token);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Avalonia.RemoteControl Android marker is not valid JSON.",
                ex);
        }
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
