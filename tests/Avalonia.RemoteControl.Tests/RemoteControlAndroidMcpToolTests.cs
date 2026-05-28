using System.Text.Json;
using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Android;
using Avalonia.RemoteControl.Tool;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlAndroidMcpToolTests
{
    [Fact]
    public async Task AndroidDeviceManagerListsAvdsAndStartsPixel6ThroughEmulatorRunner()
    {
        var adbRunner = new RecordingAdbCommandRunner();
        var sdkRunner = new RecordingAndroidCommandRunner();
        var sdkRoot = @"C:\Android\Sdk";
        var emulatorPath = Path.Combine(sdkRoot, "emulator", "emulator.exe");
        sdkRunner.Respond(
            $"{emulatorPath} -list-avds",
            new AdbCommandResult(0, "Pixel_6\nPixel_8_Pro\n", string.Empty));
        var client = new AndroidDeviceManagerClient(adbRunner, sdkRunner);

        var avds = await client.ListAvdsAsync(sdkRoot);
        var started = await client.StartAvdAsync("Pixel_6", sdkRoot, ["-no-snapshot-load"]);

        Assert.Equal(["Pixel_6", "Pixel_8_Pro"], avds);
        Assert.Equal(4242, started.ProcessId);
        Assert.Equal(emulatorPath, started.FileName);
        Assert.Equal(
            [$"{emulatorPath} -list-avds", $"{emulatorPath} -avd Pixel_6 -no-snapshot-load"],
            sdkRunner.Commands);
    }

    [Fact]
    public async Task AndroidDeviceManagerConstructsAdbDiagnosticsAndInputCommands()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond("-s emulator-5554 install -r app.apk", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 shell monkey -p com.example.app 1", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 forward tcp:47100 tcp:47101", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 shell pidof com.example.app", new AdbCommandResult(0, "1234\n", string.Empty));
        runner.Respond("-s emulator-5554 logcat -d --pid 1234 -t 50", new AdbCommandResult(0, "log line\n", string.Empty));
        runner.Respond("-s emulator-5554 shell input tap 10 20", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 shell input swipe 1 2 3 4 500", AdbCommandResult.Success);
        runner.Respond("-s emulator-5554 shell input text hello%sworld", AdbCommandResult.Success);
        var client = new AndroidDeviceManagerClient(runner, new RecordingAndroidCommandRunner());

        await client.InstallApkAsync("emulator-5554", "app.apk");
        await client.LaunchPackageAsync("emulator-5554", "com.example.app");
        var forward = await client.ForwardAsync("emulator-5554", 47100, 47101);
        var logcat = await client.ReadLogcatAsync("emulator-5554", "com.example.app", lines: 50);
        await client.TapAsync("emulator-5554", 10, 20);
        await client.SwipeAsync("emulator-5554", 1, 2, 3, 4, 500);
        await client.TextAsync("emulator-5554", "hello world");

        Assert.Equal(new Uri("http://127.0.0.1:47100"), forward.Endpoint);
        Assert.Equal("log line\n", logcat);
        Assert.Equal(
            [
                "-s emulator-5554 install -r app.apk",
                "-s emulator-5554 shell monkey -p com.example.app 1",
                "-s emulator-5554 forward tcp:47100 tcp:47101",
                "-s emulator-5554 shell pidof com.example.app",
                "-s emulator-5554 logcat -d --pid 1234 -t 50",
                "-s emulator-5554 shell input tap 10 20",
                "-s emulator-5554 shell input swipe 1 2 3 4 500",
                "-s emulator-5554 shell input text hello%sworld",
            ],
            runner.Commands);
    }

    [Fact]
    public async Task AndroidDeviceManagerCapturesScreenshotAndUiTreeThroughAdb()
    {
        var runner = new RecordingAdbCommandRunner();
        var screenshotPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "screen.png");
        runner.Respond(command => command.Contains(" shell screencap -p ", StringComparison.Ordinal), AdbCommandResult.Success);
        runner.Respond(command => command.EndsWith($" pull {runner.LastDevicePath} {screenshotPath}", StringComparison.Ordinal), AdbCommandResult.Success);
        runner.Respond(command => command.Contains(" shell rm -f ", StringComparison.Ordinal), AdbCommandResult.Success);
        runner.Respond(command => command.Contains(" shell uiautomator dump ", StringComparison.Ordinal), AdbCommandResult.Success);
        runner.Respond(command => command.EndsWith($" shell cat {runner.LastDevicePath}", StringComparison.Ordinal), new AdbCommandResult(0, "<hierarchy />", string.Empty));
        var client = new AndroidDeviceManagerClient(runner, new RecordingAndroidCommandRunner());

        var captured = await client.CaptureScreenshotAsync("emulator-5554", screenshotPath);
        var tree = await client.DumpUiTreeAsync("emulator-5554");

        Assert.Equal(screenshotPath, captured);
        Assert.Equal("<hierarchy />", tree.Xml);
        Assert.Contains(runner.Commands, command => command.Contains("shell screencap -p", StringComparison.Ordinal));
        Assert.Contains(runner.Commands, command => command.Contains("shell uiautomator dump", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AndroidDeviceManagerReportsInstallAndLaunchFailures()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 install -r broken.apk",
            new AdbCommandResult(1, string.Empty, "INSTALL_FAILED_INVALID_APK"));
        runner.Respond(
            "-s emulator-5554 shell monkey -p com.example.missing 1",
            new AdbCommandResult(1, string.Empty, "No activities found"));
        var client = new AndroidDeviceManagerClient(runner, new RecordingAndroidCommandRunner());

        var install = await Assert.ThrowsAsync<AdbCommandFailedException>(
            () => client.InstallApkAsync("emulator-5554", "broken.apk"));
        var launch = await Assert.ThrowsAsync<AdbCommandFailedException>(
            () => client.LaunchPackageAsync("emulator-5554", "com.example.missing"));

        Assert.Contains("Unable to install APK", install.Message, StringComparison.Ordinal);
        Assert.Equal("INSTALL_FAILED_INVALID_APK", install.Result.StandardError);
        Assert.Contains("Unable to launch Android package", launch.Message, StringComparison.Ordinal);
        Assert.Equal("No activities found", launch.Result.StandardError);
    }

    [Fact]
    public async Task AndroidDeviceManagerReadsUnfilteredLogcatWhenPackagePidIsMissing()
    {
        var runner = new RecordingAdbCommandRunner();
        runner.Respond(
            "-s emulator-5554 shell pidof com.example.stopped",
            new AdbCommandResult(1, string.Empty, string.Empty));
        runner.Respond(
            "-s emulator-5554 logcat -d -t 10",
            new AdbCommandResult(0, "all logs\n", string.Empty));
        var client = new AndroidDeviceManagerClient(runner, new RecordingAndroidCommandRunner());

        var logcat = await client.ReadLogcatAsync("emulator-5554", "com.example.stopped", lines: 10);

        Assert.Equal("all logs\n", logcat);
        Assert.DoesNotContain(runner.Commands, command => command.Contains("--pid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AndroidMcpToolServiceReturnsStructuredSanitizedPayloads()
    {
        var adbRunner = new RecordingAdbCommandRunner();
        var sdkRunner = new RecordingAndroidCommandRunner();
        adbRunner.Respond("devices -l", new AdbCommandResult(
            0,
            "List of devices attached\nemulator-5554 device model:Pixel_6 device:emu64x\n",
            string.Empty));
        var service = new RemoteControlAndroidMcpToolService(new AndroidDeviceManagerClient(adbRunner, sdkRunner));

        using var document = JsonDocument.Parse("{}");
        var json = await service.CallAsync(
            RemoteControlMcpToolCatalog.AndroidListDevices,
            document.RootElement);

        using var result = JsonDocument.Parse(json);
        var device = result.RootElement.GetProperty("devices").EnumerateArray().Single();
        Assert.Equal("emulator-5554", device.GetProperty("serial").GetString());
        Assert.Equal("Pixel_6", device.GetProperty("model").GetString());
    }

    [Fact]
    public async Task AndroidMcpLogcatReportsResolvedPackagePid()
    {
        var adbRunner = new RecordingAdbCommandRunner();
        var sdkRunner = new RecordingAndroidCommandRunner();
        adbRunner.Respond(
            "-s emulator-5554 shell pidof com.example.app",
            new AdbCommandResult(0, "4321\n", string.Empty));
        adbRunner.Respond(
            "-s emulator-5554 logcat -d --pid 4321 -t 25",
            new AdbCommandResult(0, "filtered log\n", string.Empty));
        var service = new RemoteControlAndroidMcpToolService(new AndroidDeviceManagerClient(adbRunner, sdkRunner));

        using var document = JsonDocument.Parse(
            """
            {
              "serial": "emulator-5554",
              "packageName": "com.example.app",
              "lines": 25
            }
            """);
        var json = await service.CallAsync(
            RemoteControlMcpToolCatalog.AndroidLogcat,
            document.RootElement);

        using var result = JsonDocument.Parse(json);
        Assert.Equal(4321, result.RootElement.GetProperty("pid").GetInt32());
        Assert.Equal("filtered log\n", result.RootElement.GetProperty("output").GetString());
    }

    private sealed class RecordingAdbCommandRunner : IAdbCommandRunner
    {
        private readonly Queue<(Func<string, bool> Matches, AdbCommandResult Result)> responses = [];

        public List<string> Commands { get; } = [];

        public string LastDevicePath { get; private set; } = string.Empty;

        public void Respond(string command, AdbCommandResult result)
        {
            responses.Enqueue((candidate => string.Equals(candidate, command, StringComparison.Ordinal), result));
        }

        public void Respond(Func<string, bool> matches, AdbCommandResult result)
        {
            responses.Enqueue((matches, result));
        }

        public Task<AdbCommandResult> RunAsync(
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            var command = string.Join(" ", arguments);
            Commands.Add(command);

            if (arguments.Count >= 6 && arguments[2] == "shell" && (arguments[3] is "screencap" or "uiautomator"))
            {
                LastDevicePath = arguments[^1];
            }

            if (responses.Count == 0)
            {
                return Task.FromResult(new AdbCommandResult(1, string.Empty, $"No fake response for {command}"));
            }

            var response = responses.Dequeue();
            return Task.FromResult(response.Matches(command)
                ? response.Result
                : new AdbCommandResult(1, string.Empty, $"Unexpected command {command}"));
        }
    }

    private sealed class RecordingAndroidCommandRunner : IAndroidCommandRunner
    {
        private readonly Dictionary<string, AdbCommandResult> responses = new(StringComparer.Ordinal);

        public List<string> Commands { get; } = [];

        public void Respond(string command, AdbCommandResult result)
        {
            responses[command] = result;
        }

        public Task<AdbCommandResult> RunAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            var command = $"{fileName} {string.Join(" ", arguments)}";
            Commands.Add(command);
            return Task.FromResult(responses.TryGetValue(command, out var result)
                ? result
                : new AdbCommandResult(1, string.Empty, $"No fake response for {command}"));
        }

        public Task<AndroidStartedProcess> StartAsync(
            string fileName,
            IReadOnlyList<string> arguments,
            CancellationToken cancellationToken = default)
        {
            Commands.Add($"{fileName} {string.Join(" ", arguments)}");
            return Task.FromResult(new AndroidStartedProcess(4242, fileName, [.. arguments]));
        }
    }
}
