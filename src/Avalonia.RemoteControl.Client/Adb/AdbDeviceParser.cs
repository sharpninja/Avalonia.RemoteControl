namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Parses adb device list output.
/// </summary>
public static class AdbDeviceParser
{
    /// <summary>
    /// Parses output from adb devices -l.
    /// </summary>
    /// <param name="output">The adb output.</param>
    /// <returns>Parsed devices.</returns>
    public static IReadOnlyList<AdbDevice> Parse(string output)
    {
        var devices = new List<AdbDevice>();

        foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 2)
            {
                continue;
            }

            var metadata = parts
                .Skip(2)
                .Select(part => part.Split(':', 2))
                .Where(pair => pair.Length == 2)
                .ToDictionary(pair => pair[0], pair => pair[1], StringComparer.Ordinal);

            devices.Add(new AdbDevice(
                parts[0],
                parts[1],
                metadata.GetValueOrDefault("product"),
                metadata.GetValueOrDefault("model"),
                metadata.GetValueOrDefault("device")));
        }

        return devices;
    }
}
