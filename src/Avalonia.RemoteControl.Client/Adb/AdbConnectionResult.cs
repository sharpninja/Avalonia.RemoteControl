using Avalonia.RemoteControl.Client.Diagnostics;
using Avalonia.RemoteControl.Client.Profiles;

namespace Avalonia.RemoteControl.Client.Adb;

/// <summary>
/// Result of an ADB-backed remote-control connection workflow.
/// </summary>
/// <param name="Forward">The created ADB forward.</param>
/// <param name="Capabilities">Capabilities returned by the authenticated probe.</param>
/// <param name="ConnectionProfile">Connection profile discovered for the forwarded endpoint.</param>
/// <param name="PackageLaunched">Whether the workflow launched the Android package.</param>
/// <param name="ProfileSaved">Whether the workflow saved the connection profile.</param>
/// <param name="ForwardRemoved">Whether the workflow removed the forward before returning.</param>
public sealed record AdbConnectionResult(
    AdbForward Forward,
    RemoteControlProbeResult Capabilities,
    RemoteControlConnectionProfile ConnectionProfile,
    bool PackageLaunched,
    bool ProfileSaved,
    bool ForwardRemoved);
