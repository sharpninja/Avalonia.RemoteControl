using Avalonia.RemoteControl.Client.Diagnostics;
using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Coordinates the ADB package discovery, forwarding, probing, and profile-save workflow.
/// </summary>
public sealed class AdbConnectionWorkflow
{
    private readonly AdbClient adbClient;
    private readonly IRemoteControlProbe remoteControlProbe;
    private readonly IRemoteControlProfileStore? profileStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="AdbConnectionWorkflow"/> class.
    /// </summary>
    /// <param name="adbClient">ADB client.</param>
    /// <param name="remoteControlProbe">Remote-control endpoint probe.</param>
    /// <param name="profileStore">Optional profile store.</param>
    public AdbConnectionWorkflow(
        AdbClient adbClient,
        IRemoteControlProbe remoteControlProbe,
        IRemoteControlProfileStore? profileStore = null)
    {
        this.adbClient = adbClient;
        this.remoteControlProbe = remoteControlProbe;
        this.profileStore = profileStore;
    }

    /// <summary>
    /// Creates and verifies an ADB-backed remote-control connection.
    /// </summary>
    /// <param name="options">Connection options.</param>
    /// <param name="progress">Optional sanitized progress reporter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The connection result.</returns>
    public async Task<AdbConnectionResult> ConnectAsync(
        AdbConnectOptions options,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Serial);
        ValidatePort(options.HostPort, nameof(options.HostPort));

        var packageName = options.PackageName;
        var token = options.Token;
        var devicePort = options.DevicePort;
        var transportProtocol = NormalizeTransportProtocol(options.TransportProtocol);
        var packageLaunched = false;

        if (!string.IsNullOrWhiteSpace(packageName))
        {
            progress?.Report($"Checking package {packageName} on {options.Serial}.");
            if (!await adbClient.IsPackageRunningAsync(options.Serial, packageName, cancellationToken)
                    .ConfigureAwait(false))
            {
                if (!options.LaunchPackageIfStopped)
                {
                    throw new InvalidOperationException(
                        $"Android package '{packageName}' is not running on device '{options.Serial}'. " +
                        "Launch the app, wait for the Avalonia.RemoteControl bridge to start, and retry.");
                }

                progress?.Report($"Launching package {packageName}.");
                await adbClient.LaunchPackageAsync(options.Serial, packageName, cancellationToken)
                    .ConfigureAwait(false);
                packageLaunched = true;

                if (!await adbClient.WaitForPackageRunningAsync(
                        options.Serial,
                        packageName,
                        options.PackageStartTimeout,
                        options.PackageStartPollInterval,
                        cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException(
                        $"Android package '{packageName}' did not start on device '{options.Serial}'.");
                }
            }
        }

        if (devicePort is null)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                throw new ArgumentException("ADB connect requires DevicePort or PackageName.");
            }

            progress?.Report($"Reading remote-control marker from {packageName}.");
            var endpointInfo = await adbClient.DiscoverEndpointAsync(
                options.Serial,
                packageName,
                cancellationToken).ConfigureAwait(false);

            if (!endpointInfo.IsGrpcProtocol && !endpointInfo.IsAndroidBridgeProtocol)
            {
                throw new InvalidOperationException(
                    $"Android marker protocol '{endpointInfo.Protocol}' is not supported by this client.");
            }

            devicePort = endpointInfo.DevicePort;
            token ??= endpointInfo.Token;
            transportProtocol = endpointInfo.Protocol;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("ADB connect requires Token or package marker token discovery.");
        }

        progress?.Report($"Creating ADB forward tcp:{options.HostPort}.");
        var forward = await adbClient.ForwardAsync(
            options.Serial,
            options.HostPort,
            devicePort.Value,
            cancellationToken).ConfigureAwait(false);

        var forwardRemoved = false;

        try
        {
            progress?.Report("Probing forwarded remote-control endpoint.");
            var capabilities = await remoteControlProbe.ProbeAsync(
                forward.Endpoint,
                token,
                transportProtocol,
                cancellationToken).ConfigureAwait(false);

            var profile = new RemoteControlConnectionProfile
            {
                AppId = packageName ?? string.Empty,
                DisplayName = packageName ?? forward.Endpoint.ToString(),
                Endpoint = forward.Endpoint.ToString(),
                Token = token,
                TransportProtocol = transportProtocol,
                ConnectionMode = "adb",
                AndroidPackageName = packageName ?? string.Empty,
                AndroidSerial = options.Serial,
                AdbHostPort = options.HostPort,
                AdbDevicePort = devicePort,
                UpdatedUtc = DateTimeOffset.UtcNow,
            };

            var profileSaved = false;
            if (options.SaveProfile && profileStore is not null)
            {
                await profileStore.SaveDefaultAsync(profile, cancellationToken).ConfigureAwait(false);
                profileSaved = true;
            }

            if (options.CleanupOnExit)
            {
                await adbClient.RemoveForwardAsync(options.Serial, options.HostPort, cancellationToken)
                    .ConfigureAwait(false);
                forwardRemoved = true;
            }

            progress?.Report("ADB remote-control connection is ready.");
            return new AdbConnectionResult(
                forward,
                capabilities,
                profile,
                packageLaunched,
                profileSaved,
                forwardRemoved);
        }
        catch
        {
            if (options.CleanupOnExit)
            {
                await adbClient.RemoveForwardAsync(options.Serial, options.HostPort, cancellationToken)
                    .ConfigureAwait(false);
            }

            throw;
        }
    }

    private static void ValidatePort(int port, string parameterName)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Port must be between 1 and 65535.");
        }
    }

    private static string NormalizeTransportProtocol(string value)
    {
        if (value.Equals(RemoteControlProtocol.GrpcTransportProtocol, StringComparison.OrdinalIgnoreCase))
        {
            return RemoteControlProtocol.GrpcTransportProtocol;
        }

        if (value.Equals(RemoteControlProtocol.AndroidBridgeTransportProtocol, StringComparison.OrdinalIgnoreCase))
        {
            return RemoteControlProtocol.AndroidBridgeTransportProtocol;
        }

        throw new ArgumentException(
            $"Unsupported transport protocol '{value}'. Supported values are grpc and arc-protobuf-v1.");
    }
}
