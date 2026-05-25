using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Client.Adb;
using Avalonia.RemoteControl.Client.Diagnostics;

if (args.Length == 0)
{
    Console.WriteLine(RemoteControlClientInfo.CreateHelpText());
    return 0;
}

if (args is ["adb", .. var adbArgs])
{
    var adbCommandLine = new AdbCommandLine(
        new AdbClient(new ProcessAdbCommandRunner()),
        new GrpcRemoteControlProbe());

    return await adbCommandLine.RunAsync(adbArgs, Console.Out, Console.Error);
}

if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(RemoteControlClientInfo.CreateHelpText());
    return 0;
}

Console.Error.WriteLine("Unsupported command. Run 'avalonia-remote --help' for available commands.");
return 2;
