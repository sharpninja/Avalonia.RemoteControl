using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Diagnostics;
using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlAdbClientTests
{
    [Fact]
    public void AdbDeviceParserParsesLongDeviceOutput()
    {
        const string output = """
        List of devices attached
        emulator-5554          device product:sdk_gphone64_x86_64 model:Pixel_8_Pro device:emu64x transport_id:1
        R58M123456A            offline transport_id:2

        """;

        var devices = AdbDeviceParser.Parse(output);

        Assert.Equal(2, devices.Count);
        Assert.Equal("emulator-5554", devices[0].Serial);
        Assert.Equal("device", devices[0].State);
        Assert.Equal("Pixel_8_Pro", devices[0].Model);
        Assert.Equal("R58M123456A", devices[1].Serial);
        Assert.Equal("offline", devices[1].State);
    }

    [Fact]
    public async Task AdbClientListsDevicesThroughRunner()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "devices -l",
            new AdbCommandResult(0, "List of devices attached\nemulator-5554 device model:Pixel\n", string.Empty));
        var client = new AdbClient(runner);

        var devices = await client.ListDevicesAsync();

        Assert.Single(devices);
        Assert.Equal("emulator-5554", devices[0].Serial);
        Assert.Equal("devices -l", Assert.Single(runner.Commands));
    }

    [Fact]
    public async Task AdbClientCreatesAndRemovesForwardWithSerial()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47100", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 forward --remove tcp:47100", AdbCommandResult.Success);
        var client = new AdbClient(runner);

        var forward = await client.ForwardAsync("emulator-5554", 47100, 47100);
        await client.RemoveForwardAsync("emulator-5554", 47100);

        Assert.Equal(new Uri("http://127.0.0.1:47100"), forward.Endpoint);
        Assert.Equal("emulator-5554", forward.Serial);
        Assert.Equal(
            ["-s emulator-5554 forward tcp:47100 tcp:47100", "-s emulator-5554 forward --remove tcp:47100"],
            runner.Commands);
    }

    [Fact]
    public async Task AdbClientDiscoversEndpointMetadataFromPackageMarker()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(0, """{"devicePort":47101,"token":"marker-token"}""", string.Empty));
        var client = new AdbClient(runner);

        var endpoint = await client.DiscoverEndpointAsync("emulator-5554", "com.example.app");

        Assert.Equal(47101, endpoint.DevicePort);
        Assert.Equal("marker-token", endpoint.Token);
        Assert.Equal(AdbEndpointInfo.GrpcProtocol, endpoint.Protocol);
        Assert.Null(endpoint.ProtocolVersion);
        Assert.True(endpoint.IsGrpcProtocol);
    }

    [Fact]
    public async Task AdbClientDiscoversVersionedBridgeMetadataFromPackageMarker()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(
                0,
                """{"schemaVersion":"1","devicePort":47102,"token":"marker-token","bridgeProtocol":"arc-protobuf-v1"}""",
                string.Empty));
        var client = new AdbClient(runner);

        var endpoint = await client.DiscoverEndpointAsync("emulator-5554", "com.example.app");

        Assert.Equal(47102, endpoint.DevicePort);
        Assert.Equal("marker-token", endpoint.Token);
        Assert.Equal(AdbEndpointInfo.AndroidBridgeProtocol, endpoint.Protocol);
        Assert.Equal("1", endpoint.ProtocolVersion);
        Assert.False(endpoint.IsGrpcProtocol);
    }

    [Fact]
    public async Task AdbClientChecksPackageRunningStateThroughPidof()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.stopped",
            new AdbCommandResult(1, string.Empty, string.Empty));
        var client = new AdbClient(runner);

        var running = await client.IsPackageRunningAsync("emulator-5554", "com.example.app");
        var stopped = await client.IsPackageRunningAsync("emulator-5554", "com.example.stopped");

        Assert.True(running);
        Assert.False(stopped);
    }

    [Fact]
    public async Task AdbCommandLineConnectCreatesForwardAndProbesEndpoint()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47100", AdbCommandResult.Success);
        var probe = new RecordingRemoteControlProbe();
        var commandLine = new AdbCommandLine(new AdbClient(runner), probe);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await commandLine.RunAsync(
            [
                "connect",
                "--serial",
                "emulator-5554",
                "--device-port",
                "47100",
                "--token",
                "dev-token",
                "--keep-forward"
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal("http://127.0.0.1:47100/", probe.Endpoint?.ToString());
        Assert.Equal("dev-token", probe.Token);
        Assert.Contains("forward ready", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Frame streaming: supported", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("Remote input: supported", output.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("dev-token", output.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AdbCommandLineConnectRemovesForwardByDefaultAfterProbe()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47100", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 forward --remove tcp:47100", AdbCommandResult.Success);
        var commandLine = new AdbCommandLine(
            new AdbClient(runner),
            new RecordingRemoteControlProbe());
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await commandLine.RunAsync(
            [
                "connect",
                "--serial",
                "emulator-5554",
                "--device-port",
                "47100",
                "--token",
                "dev-token"
            ],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal(
            ["-s emulator-5554 forward tcp:47100 tcp:47100", "-s emulator-5554 forward --remove tcp:47100"],
            runner.Commands);
        Assert.Contains("removed", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdbCommandLineConnectSupportsBridgeMarkerProtocol()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(
                0,
                """{"devicePort":47102,"token":"marker-token","bridgeProtocol":"arc-protobuf-v1"}""",
                string.Empty));
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47102", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 forward --remove tcp:47100", AdbCommandResult.Success);
        var probe = new RecordingRemoteControlProbe();
        var commandLine = new AdbCommandLine(
            new AdbClient(runner),
            probe);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await commandLine.RunAsync(
            ["connect", "--serial", "emulator-5554", "--package", "com.example.app"],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Equal(AdbEndpointInfo.AndroidBridgeProtocol, probe.TransportProtocol);
        Assert.Equal(
            [
                "-s emulator-5554 shell pidof com.example.app",
                "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
                "-s emulator-5554 forward tcp:47100 tcp:47102",
                "-s emulator-5554 forward --remove tcp:47100"
            ],
            runner.Commands);
    }

    [Fact]
    public async Task AdbCommandLineConnectLaunchesStoppedPackageBeforeForwarding()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(1, string.Empty, string.Empty));
        runner.Respond(
            "-s emulator-5554 shell monkey -p com.example.app 1",
            AdbCommandResult.Success);
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(
                0,
                """{"devicePort":47102,"token":"marker-token","bridgeProtocol":"arc-protobuf-v1"}""",
                string.Empty));
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47102", AdbCommandResult.Success);
        var commandLine = new AdbCommandLine(
            new AdbClient(runner),
            new RecordingRemoteControlProbe());
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await commandLine.RunAsync(
            ["connect", "--serial", "emulator-5554", "--package", "com.example.app", "--keep-forward"],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("package launched", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            [
                "-s emulator-5554 shell pidof com.example.app",
                "-s emulator-5554 shell monkey -p com.example.app 1",
                "-s emulator-5554 shell pidof com.example.app",
                "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
                "-s emulator-5554 forward tcp:47100 tcp:47102"
            ],
            runner.Commands);
    }

    [Fact]
    public async Task AdbCommandLineConnectKeepForwardSavesBridgeConnectionProfile()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(
                0,
                """{"devicePort":47102,"token":"marker-token","bridgeProtocol":"arc-protobuf-v1"}""",
                string.Empty));
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47102", AdbCommandResult.Success);
        var profilePath = Path.Combine(
            Path.GetTempPath(),
            "Avalonia.RemoteControl.Tests",
            Guid.NewGuid().ToString("N"),
            "connection-profile.json");
        var profileStore = new FileRemoteControlProfileStore(profilePath);
        var commandLine = new AdbCommandLine(
            new AdbClient(runner),
            new RecordingRemoteControlProbe(),
            profileStore);
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await commandLine.RunAsync(
            ["connect", "--serial", "emulator-5554", "--package", "com.example.app", "--keep-forward"],
            output,
            error);
        var profile = await profileStore.LoadDefaultAsync();

        Assert.Equal(0, exitCode);
        Assert.NotNull(profile);
        Assert.Equal("http://127.0.0.1:47100/", profile.Endpoint);
        Assert.Equal("marker-token", profile.Token);
        Assert.Equal(RemoteControlProtocol.AndroidBridgeTransportProtocol, profile.TransportProtocol);
        Assert.Contains("profile saved", output.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-s emulator-5554 shell pidof com.example.app", runner.Commands, StringComparer.Ordinal);
    }

    [Fact]
    public async Task AdbConnectionWorkflowLaunchesStoppedPackageAndSavesProfile()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(1, string.Empty, string.Empty));
        runner.Respond(
            "-s emulator-5554 shell monkey -p com.example.app 1",
            AdbCommandResult.Success);
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(
                0,
                """{"devicePort":47102,"token":"marker-token","bridgeProtocol":"arc-protobuf-v1"}""",
                string.Empty));
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47102", AdbCommandResult.Success);
        var profilePath = Path.Combine(
            Path.GetTempPath(),
            "Avalonia.RemoteControl.Tests",
            Guid.NewGuid().ToString("N"),
            "connection-profile.json");
        var profileStore = new FileRemoteControlProfileStore(profilePath);
        var progressMessages = new List<string>();
        var workflow = new AdbConnectionWorkflow(
            new AdbClient(runner),
            new RecordingRemoteControlProbe(),
            profileStore);

        var result = await workflow.ConnectAsync(
            new AdbConnectOptions
            {
                Serial = "emulator-5554",
                PackageName = "com.example.app",
                LaunchPackageIfStopped = true,
                SaveProfile = true,
                CleanupOnExit = false,
                PackageStartTimeout = TimeSpan.FromSeconds(1),
                PackageStartPollInterval = TimeSpan.FromMilliseconds(1),
            },
            new InlineProgress<string>(progressMessages.Add));
        var profile = await profileStore.LoadDefaultAsync();

        Assert.True(result.PackageLaunched);
        Assert.True(result.ProfileSaved);
        Assert.False(result.ForwardRemoved);
        Assert.Equal("http://127.0.0.1:47100/", result.ConnectionProfile.Endpoint);
        Assert.Equal(RemoteControlProtocol.AndroidBridgeTransportProtocol, result.ConnectionProfile.TransportProtocol);
        Assert.NotNull(profile);
        Assert.Equal("marker-token", profile.Token);
        Assert.DoesNotContain(progressMessages, message => message.Contains("marker-token", StringComparison.Ordinal));
        Assert.Equal(
            [
                "-s emulator-5554 shell pidof com.example.app",
                "-s emulator-5554 shell monkey -p com.example.app 1",
                "-s emulator-5554 shell pidof com.example.app",
                "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
                "-s emulator-5554 forward tcp:47100 tcp:47102"
            ],
            runner.Commands);
    }

    [Fact]
    public async Task AdbConnectionWorkflowMarkerTokenOverridesStaleSuppliedToken()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond(
            "-s emulator-5554 shell run-as com.example.app cat files/avalonia-remote-control.json",
            new AdbCommandResult(
                0,
                """{"devicePort":47102,"token":"marker-token","bridgeProtocol":"arc-protobuf-v1"}""",
                string.Empty));
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47102", AdbCommandResult.Success);
        var probe = new RecordingRemoteControlProbe();
        var workflow = new AdbConnectionWorkflow(
            new AdbClient(runner),
            probe,
            new FileRemoteControlProfileStore(Path.Combine(
                Path.GetTempPath(),
                "Avalonia.RemoteControl.Tests",
                Guid.NewGuid().ToString("N"),
                "connection-profile.json")));

        var result = await workflow.ConnectAsync(
            new AdbConnectOptions
            {
                Serial = "emulator-5554",
                PackageName = "com.example.app",
                HostPort = 47100,
                Token = "stale-token",
                LaunchPackageIfStopped = true,
                CleanupOnExit = false,
            });

        Assert.Equal("marker-token", probe.Token);
        Assert.Equal("marker-token", result.ConnectionProfile.Token);
        Assert.Equal(AdbEndpointInfo.AndroidBridgeProtocol, probe.TransportProtocol);
    }

    [Fact]
    public async Task AdbConnectionWorkflowCanSaveExplicitBridgeForwardProfile()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47100", AdbCommandResult.Success);
        var profilePath = Path.Combine(
            Path.GetTempPath(),
            "Avalonia.RemoteControl.Tests",
            Guid.NewGuid().ToString("N"),
            "connection-profile.json");
        var profileStore = new FileRemoteControlProfileStore(profilePath);
        var workflow = new AdbConnectionWorkflow(
            new AdbClient(runner),
            new RecordingRemoteControlProbe(),
            profileStore);

        var result = await workflow.ConnectAsync(new AdbConnectOptions
        {
            Serial = "emulator-5554",
            DevicePort = 47100,
            HostPort = 47100,
            Token = "dev-token",
            TransportProtocol = RemoteControlProtocol.AndroidBridgeTransportProtocol,
            SaveProfile = true,
            CleanupOnExit = false,
        });
        var profile = await profileStore.LoadDefaultAsync();

        Assert.False(result.PackageLaunched);
        Assert.False(result.ForwardRemoved);
        Assert.True(result.ProfileSaved);
        Assert.NotNull(profile);
        Assert.Equal("emulator-5554", profile.AndroidSerial);
        Assert.Equal(47100, profile.AdbHostPort);
        Assert.Equal(47100, profile.AdbDevicePort);
        Assert.Equal("adb", profile.ConnectionMode);
        Assert.Equal(RemoteControlProtocol.AndroidBridgeTransportProtocol, profile.TransportProtocol);
        Assert.Equal(["-s emulator-5554 forward tcp:47100 tcp:47100"], runner.Commands);
    }

    [Fact]
    public async Task AdbCommandLineCleanupRemovesForward()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond("-s emulator-5554 forward --remove tcp:47100", AdbCommandResult.Success);
        var commandLine = new AdbCommandLine(
            new AdbClient(runner),
            new RecordingRemoteControlProbe());
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = await commandLine.RunAsync(
            ["cleanup", "--serial", "emulator-5554", "--host-port", "47100"],
            output,
            error);

        Assert.Equal(0, exitCode);
        Assert.Contains("removed", output.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingAdbCommandRunner : IAdbCommandRunner
    {
        private readonly Dictionary<string, Queue<AdbCommandResult>> responses = new(StringComparer.Ordinal);

        public List<string> Commands { get; } = [];

        public void Respond(string command, AdbCommandResult result)
        {
            if (!responses.TryGetValue(command, out var queue))
            {
                queue = new Queue<AdbCommandResult>();
                responses[command] = queue;
            }

            queue.Enqueue(result);
        }

        public Task<AdbCommandResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            var command = string.Join(" ", arguments);
            Commands.Add(command);

            return Task.FromResult(responses.TryGetValue(command, out var queue) && queue.Count > 0
                ? queue.Dequeue()
                : new AdbCommandResult(1, string.Empty, $"No fake response for {command}"));
        }
    }

    private sealed class RecordingRemoteControlProbe : IRemoteControlProbe
    {
        public Uri? Endpoint { get; private set; }

        public string? Token { get; private set; }

        public string? TransportProtocol { get; private set; }

        public Task<RemoteControlProbeResult> ProbeAsync(
            Uri endpoint,
            string token,
            string transportProtocol,
            CancellationToken cancellationToken = default)
        {
            Endpoint = endpoint;
            Token = token;
            TransportProtocol = transportProtocol;

            return Task.FromResult(new RemoteControlProbeResult(
                RemoteControlProtocol.DisplayVersion,
                "remote-client",
                true,
                true,
                true,
                true,
                true,
                true,
                true));
        }
    }

    private sealed class InlineProgress<T>(Action<T> onReport) : IProgress<T>
    {
        public void Report(T value)
        {
            onReport(value);
        }
    }
}
