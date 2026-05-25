using Avalonia.RemoteControl.Client;

if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine(RemoteControlClientInfo.CreateHelpText());
    return 0;
}

if (args is ["adb", "list"])
{
    Console.WriteLine("ADB discovery is planned for Iteration 5. See docs/architecture/android-adb-connectivity.md.");
    return 0;
}

Console.Error.WriteLine("Unsupported command. Run 'avalonia-remote --help' for available commands.");
return 2;
