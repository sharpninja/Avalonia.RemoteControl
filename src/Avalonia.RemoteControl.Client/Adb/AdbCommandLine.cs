using Avalonia.RemoteControl.Client.Diagnostics;
using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Implements adb subcommands for the avalonia-remote tool.
/// </summary>
public sealed class AdbCommandLine
{
    private readonly AdbClient adbClient;
    private readonly IRemoteControlProbe remoteControlProbe;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdbCommandLine"/> class.
    /// </summary>
    /// <param name="adbClient">ADB client.</param>
    /// <param name="remoteControlProbe">Remote-control probe.</param>
    public AdbCommandLine(
        AdbClient adbClient,
        IRemoteControlProbe remoteControlProbe)
    {
        this.adbClient = adbClient;
        this.remoteControlProbe = remoteControlProbe;
    }

    /// <summary>
    /// Runs an adb subcommand.
    /// </summary>
    /// <param name="args">Arguments after the adb verb.</param>
    /// <param name="output">Standard output.</param>
    /// <param name="error">Standard error.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Process exit code.</returns>
    public async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return args.Count == 0 || IsHelp(args[0])
                ? await WriteHelpAsync(output).ConfigureAwait(false)
                : args[0] switch
                {
                    "list" => await ListAsync(output, cancellationToken).ConfigureAwait(false),
                    "connect" => await ConnectAsync(args.Skip(1).ToArray(), output, error, cancellationToken)
                        .ConfigureAwait(false),
                    "cleanup" => await CleanupAsync(args.Skip(1).ToArray(), output, error, cancellationToken)
                        .ConfigureAwait(false),
                    _ => await WriteUsageErrorAsync(error, "Unsupported adb command.").ConfigureAwait(false),
                };
        }
        catch (AdbCommandFailedException ex)
        {
            await error.WriteLineAsync($"{ex.Message} {SanitizeAdbError(ex.Result.StandardError)}").ConfigureAwait(false);
            return 1;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or InvalidOperationException)
        {
            await error.WriteLineAsync(ex.Message).ConfigureAwait(false);
            return 1;
        }
    }

    private async Task<int> ListAsync(
        TextWriter output,
        CancellationToken cancellationToken)
    {
        var devices = await adbClient.ListDevicesAsync(cancellationToken).ConfigureAwait(false);

        if (devices.Count == 0)
        {
            await output.WriteLineAsync("No ADB devices or emulators were found.").ConfigureAwait(false);
            return 0;
        }

        foreach (var device in devices)
        {
            await output.WriteLineAsync(
                $"{device.Serial}\t{device.State}\tmodel:{device.Model ?? "-"}\tdevice:{device.Device ?? "-"}")
                .ConfigureAwait(false);
        }

        return 0;
    }

    private async Task<int> ConnectAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var parsed = ParseOptions(args);
        var serial = GetRequired(parsed, "serial");
        var hostPort = GetOptionalPort(parsed, "host-port") ?? RemoteControlProtocol.DefaultPort;
        var devicePort = GetOptionalPort(parsed, "device-port");
        var packageName = GetOptional(parsed, "package");
        var token = GetOptional(parsed, "token");
        var keepForward = parsed.ContainsKey("keep-forward");

        if (devicePort is null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return await WriteUsageErrorAsync(
                    error,
                    "adb connect requires --device-port or --package.")
                    .ConfigureAwait(false);
            }

            var endpointInfo = await adbClient.DiscoverEndpointAsync(
                serial,
                packageName,
                cancellationToken).ConfigureAwait(false);
            if (!endpointInfo.IsGrpcProtocol)
            {
                throw new InvalidOperationException(
                    $"Android marker protocol '{endpointInfo.Protocol}' is not supported by this client yet.");
            }

            devicePort = endpointInfo.DevicePort;
            token ??= endpointInfo.Token;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return await WriteUsageErrorAsync(
                error,
                "adb connect requires --token or package marker token discovery.")
                .ConfigureAwait(false);
        }

        var forward = await adbClient.ForwardAsync(
            serial,
            hostPort,
            devicePort.Value,
            cancellationToken).ConfigureAwait(false);

        try
        {
            var capabilities = await remoteControlProbe.ProbeAsync(
                forward.Endpoint,
                token,
                cancellationToken).ConfigureAwait(false);

            await output.WriteLineAsync("ADB forward ready.").ConfigureAwait(false);
            await output.WriteLineAsync($"Serial: {forward.Serial}").ConfigureAwait(false);
            await output.WriteLineAsync($"Endpoint: {forward.Endpoint}").ConfigureAwait(false);
            await output.WriteLineAsync($"Protocol: {capabilities.ProtocolVersion}").ConfigureAwait(false);

            if (!keepForward)
            {
                await adbClient.RemoveForwardAsync(serial, hostPort, cancellationToken).ConfigureAwait(false);
                await output.WriteLineAsync("Forward removed after successful probe.").ConfigureAwait(false);
            }

            return 0;
        }
        catch
        {
            if (!keepForward)
            {
                await adbClient.RemoveForwardAsync(serial, hostPort, cancellationToken).ConfigureAwait(false);
            }

            throw;
        }
    }

    private async Task<int> CleanupAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var parsed = ParseOptions(args);
        var serial = GetRequired(parsed, "serial");
        var hostPort = GetOptionalPort(parsed, "host-port") ?? RemoteControlProtocol.DefaultPort;

        await adbClient.RemoveForwardAsync(serial, hostPort, cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync($"ADB forward tcp:{hostPort} removed for {serial}.").ConfigureAwait(false);
        return 0;
    }

    private static async Task<int> WriteHelpAsync(TextWriter output)
    {
        await output.WriteLineAsync(
            """
            Usage:
              avalonia-remote adb list
              avalonia-remote adb connect --serial <serial> --device-port <port> --token <token> [--host-port <port>] [--keep-forward]
              avalonia-remote adb connect --serial <serial> --package <package> [--token <token>] [--host-port <port>] [--keep-forward]
              avalonia-remote adb cleanup --serial <serial> [--host-port <port>]
            """).ConfigureAwait(false);

        return 0;
    }

    private static async Task<int> WriteUsageErrorAsync(TextWriter error, string message)
    {
        await error.WriteLineAsync(message).ConfigureAwait(false);
        await error.WriteLineAsync("Run 'avalonia-remote adb --help' for usage.").ConfigureAwait(false);
        return 2;
    }

    private static Dictionary<string, string?> ParseOptions(IReadOnlyList<string> args)
    {
        var parsed = new Dictionary<string, string?>(StringComparer.Ordinal);

        for (var index = 0; index < args.Count; index++)
        {
            var current = args[index];

            if (!current.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{current}'.");
            }

            var key = current[2..];

            if (key is "keep-forward")
            {
                parsed[key] = null;
                continue;
            }

            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Missing value for --{key}.");
            }

            parsed[key] = args[++index];
        }

        return parsed;
    }

    private static string GetRequired(IReadOnlyDictionary<string, string?> parsed, string key)
    {
        var value = GetOptional(parsed, key);
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"Missing required option --{key}.")
            : value;
    }

    private static string? GetOptional(IReadOnlyDictionary<string, string?> parsed, string key)
    {
        return parsed.TryGetValue(key, out var value) ? value : null;
    }

    private static int? GetOptionalPort(IReadOnlyDictionary<string, string?> parsed, string key)
    {
        var value = GetOptional(parsed, key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value, out var parsedPort)
            ? parsedPort
            : throw new ArgumentException($"--{key} must be a valid port.");
    }

    private static bool IsHelp(string value)
    {
        return value.Equals("--help", StringComparison.OrdinalIgnoreCase)
            || value.Equals("-h", StringComparison.OrdinalIgnoreCase)
            || value.Equals("help", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeAdbError(string standardError)
    {
        return string.IsNullOrWhiteSpace(standardError)
            ? string.Empty
            : standardError.Trim();
    }
}
