using Avalonia.RemoteControl.Protocol.V1;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Avalonia.RemoteControl.Client.Bridge;
using Avalonia.RemoteControl.Protocol;
using Grpc.Net.Client;

namespace Avalonia.RemoteControl.Client;

/// <summary>
/// Provides a token-authenticated client session for desktop remote-control clients.
/// </summary>
public sealed class RemoteControlDesktopSession : IDisposable
{
    private readonly GrpcChannel? channel;
    private readonly Protocol.V1.RemoteControl.RemoteControlClient? client;
    private readonly global::Grpc.Core.Metadata? headers;
    private readonly RemoteControlBridgeClient? bridgeClient;
    private readonly X509Certificate2? trustedServerCertificate;

    private RemoteControlDesktopSession(
        Uri endpoint,
        string token,
        string? trustedServerCertificatePath,
        string transportProtocol)
    {
        Endpoint = endpoint;

        if (transportProtocol.Equals(
            RemoteControlProtocol.AndroidBridgeTransportProtocol,
            StringComparison.OrdinalIgnoreCase))
        {
            bridgeClient = new RemoteControlBridgeClient(endpoint, token);
        }
        else if (transportProtocol.Equals(
            RemoteControlProtocol.GrpcTransportProtocol,
            StringComparison.OrdinalIgnoreCase))
        {
            trustedServerCertificate = LoadTrustedServerCertificate(trustedServerCertificatePath);
            channel = CreateChannel(endpoint, trustedServerCertificate);
            client = new Protocol.V1.RemoteControl.RemoteControlClient(channel);
            headers = new global::Grpc.Core.Metadata
            {
                { "authorization", $"Bearer {token}" },
            };
        }
        else
        {
            throw new ArgumentException(
                $"Unsupported remote-control transport protocol '{transportProtocol}'.",
                nameof(transportProtocol));
        }
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
    /// <param name="trustedServerCertificatePath">Optional certificate file whose thumbprint is trusted for TLS connections.</param>
    /// <returns>A connected session object.</returns>
    public static RemoteControlDesktopSession Create(
        Uri endpoint,
        string token,
        string? trustedServerCertificatePath = null,
        string transportProtocol = RemoteControlProtocol.GrpcTransportProtocol)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        ArgumentException.ThrowIfNullOrWhiteSpace(transportProtocol);

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        return new RemoteControlDesktopSession(
            endpoint,
            token,
            trustedServerCertificatePath,
            transportProtocol);
    }

    /// <summary>
    /// Gets endpoint capabilities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Capabilities response.</returns>
    public async Task<GetCapabilitiesResponse> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default)
    {
        if (bridgeClient is not null)
        {
            return await bridgeClient.GetCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await client!.GetCapabilitiesAsync(
            new GetCapabilitiesRequest(),
            headers!,
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
        if (bridgeClient is not null)
        {
            return await bridgeClient.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
        }

        return await client!.GetSnapshotAsync(
            new GetSnapshotRequest(),
            headers!,
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
        if (bridgeClient is not null)
        {
            return await bridgeClient.InvokeClickAsync(nodeId, cancellationToken).ConfigureAwait(false);
        }

        return await client!.InvokeClickAsync(
            new InvokeClickRequest { NodeId = nodeId },
            headers!,
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
        if (bridgeClient is not null)
        {
            return await bridgeClient.InvokeFocusAsync(nodeId, cancellationToken).ConfigureAwait(false);
        }

        return await client!.InvokeFocusAsync(
            new InvokeFocusRequest { NodeId = nodeId },
            headers!,
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
        if (bridgeClient is not null)
        {
            return await bridgeClient.SetPropertyAsync(
                nodeId,
                propertyName,
                value,
                cancellationToken).ConfigureAwait(false);
        }

        return await client!.SetPropertyAsync(
            new SetPropertyRequest
            {
                NodeId = nodeId,
                PropertyName = propertyName,
                Value = value,
            },
            headers!,
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
        if (bridgeClient is not null)
        {
            return bridgeClient.WatchLogsAsync(minimumLevel, categoryPrefix, cancellationToken);
        }

        var call = client!.WatchLogs(
            new WatchLogsRequest
            {
                MinimumLevel = minimumLevel,
                CategoryPrefix = categoryPrefix ?? string.Empty,
            },
            headers!,
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
        bridgeClient?.Dispose();
        channel?.Dispose();
        trustedServerCertificate?.Dispose();
    }

    private static X509Certificate2? LoadTrustedServerCertificate(string? trustedServerCertificatePath)
    {
        if (string.IsNullOrWhiteSpace(trustedServerCertificatePath))
        {
            return null;
        }

        return X509CertificateLoader.LoadCertificateFromFile(trustedServerCertificatePath);
    }

    private static GrpcChannel CreateChannel(
        Uri endpoint,
        X509Certificate2? trustedServerCertificate)
    {
        if (trustedServerCertificate is null)
        {
            return GrpcChannel.ForAddress(endpoint);
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, _) =>
                IsTrustedServerCertificate(certificate, trustedServerCertificate),
        };

        return GrpcChannel.ForAddress(
            endpoint,
            new GrpcChannelOptions { HttpHandler = handler });
    }

    private static bool IsTrustedServerCertificate(
        X509Certificate2? certificate,
        X509Certificate2 trustedServerCertificate)
    {
        if (certificate is null)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            certificate.GetCertHash(HashAlgorithmName.SHA256),
            trustedServerCertificate.GetCertHash(HashAlgorithmName.SHA256));
    }
}
