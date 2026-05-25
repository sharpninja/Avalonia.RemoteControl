using Avalonia.RemoteControl.Protocol.V1;
using System.Runtime.CompilerServices;
using Grpc.Net.Client;

namespace Avalonia.RemoteControl.Client;

/// <summary>
/// Provides a token-authenticated gRPC client session for desktop remote-control clients.
/// </summary>
public sealed class RemoteControlDesktopSession : IDisposable
{
    private readonly GrpcChannel channel;
    private readonly Protocol.V1.RemoteControl.RemoteControlClient client;
    private readonly global::Grpc.Core.Metadata headers;

    private RemoteControlDesktopSession(
        Uri endpoint,
        string token)
    {
        Endpoint = endpoint;
        channel = GrpcChannel.ForAddress(endpoint);
        client = new Protocol.V1.RemoteControl.RemoteControlClient(channel);
        headers = new global::Grpc.Core.Metadata
        {
            { "authorization", $"Bearer {token}" },
        };
    }

    /// <summary>
    /// Gets the endpoint URI.
    /// </summary>
    public Uri Endpoint { get; }

    /// <summary>
    /// Creates a desktop client session.
    /// </summary>
    /// <param name="endpoint">Remote endpoint.</param>
    /// <param name="token">Bearer token.</param>
    /// <returns>A connected session object.</returns>
    public static RemoteControlDesktopSession Create(Uri endpoint, string token)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        return new RemoteControlDesktopSession(endpoint, token);
    }

    /// <summary>
    /// Gets endpoint capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Capabilities response.</returns>
    public async Task<GetCapabilitiesResponse> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        return await client.GetCapabilitiesAsync(
            new GetCapabilitiesRequest(),
            headers,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Gets the current remote tree snapshot.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tree snapshot.</returns>
    public async Task<TreeSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        return await client.GetSnapshotAsync(
            new GetSnapshotRequest(),
            headers,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Invokes a click on a remote node.
    /// </summary>
    /// <param name="nodeId">Stable remote node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    public async Task<CommandResult> InvokeClickAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        return await client.InvokeClickAsync(
            new InvokeClickRequest { NodeId = nodeId },
            headers,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Requests focus on a remote node.
    /// </summary>
    /// <param name="nodeId">Stable remote node ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    public async Task<CommandResult> InvokeFocusAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        return await client.InvokeFocusAsync(
            new InvokeFocusRequest { NodeId = nodeId },
            headers,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Sets a remote property.
    /// </summary>
    /// <param name="nodeId">Stable remote node ID.</param>
    /// <param name="propertyName">Property name.</param>
    /// <param name="value">String value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Command result.</returns>
    public async Task<CommandResult> SetPropertyAsync(
        string nodeId,
        string propertyName,
        string value,
        CancellationToken cancellationToken = default)
    {
        return await client.SetPropertyAsync(
            new SetPropertyRequest
            {
                NodeId = nodeId,
                PropertyName = propertyName,
                Value = value,
            },
            headers,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Watches remote log entries.
    /// </summary>
    /// <param name="minimumLevel">Minimum log level.</param>
    /// <param name="categoryPrefix">Optional category prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Log stream.</returns>
    public IAsyncEnumerable<LogEntry> WatchLogsAsync(
        string minimumLevel,
        string? categoryPrefix,
        CancellationToken cancellationToken = default)
    {
        var call = client.WatchLogs(
            new WatchLogsRequest
            {
                MinimumLevel = minimumLevel,
                CategoryPrefix = categoryPrefix ?? string.Empty,
            },
            headers,
            cancellationToken: cancellationToken);

        return ReadLogStreamAsync(call.ResponseStream, cancellationToken);
    }

    private static async IAsyncEnumerable<LogEntry> ReadLogStreamAsync(
        global::Grpc.Core.IAsyncStreamReader<LogEntry> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (await reader.MoveNext(cancellationToken).ConfigureAwait(false))
        {
            yield return reader.Current;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        channel.Dispose();
    }
}
