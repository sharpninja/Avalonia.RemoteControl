using Avalonia;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Diagnostics;

namespace Avalonia.RemoteControl.Tool;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        return RunCliAsync(args).GetAwaiter().GetResult();
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    private static async Task<int> RunCliAsync(string[] args)
    {
        if (args is ["adb", .. var adbArgs])
        {
            var adbCommandLine = new AdbCommandLine(
                new AdbClient(new ProcessAdbCommandRunner()),
                new GrpcRemoteControlProbe());

            return await adbCommandLine.RunAsync(adbArgs, Console.Out, Console.Error).ConfigureAwait(false);
        }

        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase)
            || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine(RemoteControlClientInfo.CreateHelpText());
            return 0;
        }

        Console.Error.WriteLine("Unsupported command. Run 'avalonia-remote --help' for available commands.");
        return 2;
    }
}
