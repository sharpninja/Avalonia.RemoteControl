using Avalonia.RemoteControl.Client;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Creates sessions for the tool-side MCP server.
/// </summary>
public interface IRemoteControlMcpSessionFactory
{
    /// <summary>
    /// Creates an MCP session adapter.
    /// </summary>
    /// <param name="options">Connection options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Session adapter.</returns>
    Task<IRemoteControlMcpSession> CreateAsync(
        RemoteControlMcpOptions options,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Default session factory backed by <see cref="RemoteControlDesktopSession"/>.
/// </summary>
public sealed class RemoteControlMcpSessionFactory : IRemoteControlMcpSessionFactory
{
    /// <inheritdoc />
    public Task<IRemoteControlMcpSession> CreateAsync(
        RemoteControlMcpOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var session = RemoteControlDesktopSession.Create(
            options.Endpoint,
            options.Token,
            options.CertificatePath,
            options.TransportProtocol,
            options.AcceptedServerCertificateSha256Fingerprint);

        return Task.FromResult<IRemoteControlMcpSession>(new RemoteControlMcpSessionAdapter(session));
    }
}
