using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Live;

/// <summary>
/// Describes endpoint capabilities that affect the live remote view window.
/// </summary>
public sealed record RemoteLiveViewCapabilities
{
    /// <summary>
    /// Gets a capability set with no live-view support.
    /// </summary>
    public static RemoteLiveViewCapabilities None { get; } = new();

    /// <summary>
    /// Gets the remote protocol version.
    /// </summary>
    public string ProtocolVersion { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether one-shot tree snapshots are supported.
    /// </summary>
    public bool SupportsTreeSnapshots { get; init; }

    /// <summary>
    /// Gets a value indicating whether live tree streaming is supported.
    /// </summary>
    public bool SupportsTreeStreaming { get; init; }

    /// <summary>
    /// Gets a value indicating whether live frame streaming is supported.
    /// </summary>
    public bool SupportsFrameStreaming { get; init; }

    /// <summary>
    /// Gets a value indicating whether remote input is supported and enabled.
    /// </summary>
    public bool SupportsRemoteInput { get; init; }

    /// <summary>
    /// Creates live-view capabilities from the protocol capability response.
    /// </summary>
    /// <param name="capabilities">Protocol capability response.</param>
    /// <returns>Live-view capability subset.</returns>
    public static RemoteLiveViewCapabilities FromProtocol(GetCapabilitiesResponse capabilities)
    {
        ArgumentNullException.ThrowIfNull(capabilities);

        return new RemoteLiveViewCapabilities
        {
            ProtocolVersion = capabilities.ProtocolVersion,
            SupportsTreeSnapshots = capabilities.SupportsTreeSnapshots,
            SupportsTreeStreaming = capabilities.SupportsTreeStreaming,
            SupportsFrameStreaming = capabilities.SupportsFrameStreaming,
            SupportsRemoteInput = capabilities.SupportsRemoteInput,
        };
    }
}
