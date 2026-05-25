using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Diagnostics;
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
        private readonly Dictionary<string, AdbCommandResult> responses = new(StringComparer.Ordinal);

        public List<string> Commands { get; } = [];

        public void Respond(string command, AdbCommandResult result)
        {
            responses[command] = result;
        }

        public Task<AdbCommandResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            var command = string.Join(" ", arguments);
            Commands.Add(command);

            return Task.FromResult(responses.TryGetValue(command, out var result)
                ? result
                : new AdbCommandResult(1, string.Empty, $"No fake response for {command}"));
        }
    }

    private sealed class RecordingRemoteControlProbe : IRemoteControlProbe
    {
        public Uri? Endpoint { get; private set; }

        public string? Token { get; private set; }

        public Task<RemoteControlProbeResult> ProbeAsync(
            Uri endpoint,
            string token,
            CancellationToken cancellationToken = default)
        {
            Endpoint = endpoint;
            Token = token;

            return Task.FromResult(new RemoteControlProbeResult(
                RemoteControlProtocol.DisplayVersion,
                true,
                true,
                true,
                true,
                true));
        }
    }
}
