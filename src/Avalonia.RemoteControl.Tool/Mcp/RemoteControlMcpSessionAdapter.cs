using Avalonia.RemoteControl.Client;
using Google.Protobuf;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Adapts <see cref="RemoteControlDesktopSession"/> operations to JSON payloads returned through MCP.
/// </summary>
public sealed class RemoteControlMcpSessionAdapter : IRemoteControlMcpSession
{
    private readonly RemoteControlDesktopSession session;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlMcpSessionAdapter"/> class.
    /// </summary>
    /// <param name="session">Remote-control desktop session.</param>
    public RemoteControlMcpSessionAdapter(RemoteControlDesktopSession session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
    }

    /// <inheritdoc />
    public async Task<string> GetCapabilitiesJsonAsync(CancellationToken cancellationToken = default)
    {
        return JsonFormatter.Default.Format(await session.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<string> GetSnapshotJsonAsync(CancellationToken cancellationToken = default)
    {
        return JsonFormatter.Default.Format(await session.GetSnapshotAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<string> InvokeClickJsonAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        return JsonFormatter.Default.Format(await session.InvokeClickAsync(nodeId, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<string> FocusJsonAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        return JsonFormatter.Default.Format(await session.InvokeFocusAsync(nodeId, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<string> SetPropertyJsonAsync(
        string nodeId,
        string propertyName,
        string value,
        CancellationToken cancellationToken = default)
    {
        return JsonFormatter.Default.Format(
            await session.SetPropertyAsync(nodeId, propertyName, value, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        session.Dispose();
    }
}
