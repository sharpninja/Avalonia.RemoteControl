namespace Avalonia.RemoteControl.Client.Diagnostics;

/// <summary>
/// Represents capability data returned by a remote-control endpoint probe.
/// </summary>
public sealed record RemoteControlProbeResult(
    string ProtocolVersion,
    bool SupportsTreeSnapshots,
    bool SupportsTreeStreaming,
    bool SupportsClickInvocation,
    bool SupportsPropertyMutation,
    bool SupportsLogStreaming,
    bool SupportsFrameStreaming,
    bool SupportsRemoteInput);
