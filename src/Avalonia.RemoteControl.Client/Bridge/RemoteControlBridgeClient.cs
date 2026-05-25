using System.Net.Sockets;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Google.Protobuf;

namespace Avalonia.RemoteControl.Client.Bridge;

internal sealed class RemoteControlBridgeClient : IDisposable
{
    private readonly Uri endpoint;
    private readonly string authorization;

    public RemoteControlBridgeClient(Uri endpoint, string token)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (endpoint.Port < 1)
        {
            throw new ArgumentException("Bridge endpoint must include a TCP port.", nameof(endpoint));
        }

        this.endpoint = endpoint;
        authorization = RemoteControlBridgeProtocol.CreateBearerAuthorization(token);
    }

    public Task<GetCapabilitiesResponse> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return SendUnaryAsync(
            BridgeMethod.GetCapabilities,
            new GetCapabilitiesRequest(),
            GetCapabilitiesResponse.Parser,
            cancellationToken);
    }

    public Task<TreeSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        return SendUnaryAsync(
            BridgeMethod.GetSnapshot,
            new GetSnapshotRequest(),
            TreeSnapshot.Parser,
            cancellationToken);
    }

    public Task<CommandResult> InvokeClickAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        return SendUnaryAsync(
            BridgeMethod.InvokeClick,
            new InvokeClickRequest { NodeId = nodeId },
            CommandResult.Parser,
            cancellationToken);
    }

    public Task<CommandResult> InvokeFocusAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        return SendUnaryAsync(
            BridgeMethod.InvokeFocus,
            new InvokeFocusRequest { NodeId = nodeId },
            CommandResult.Parser,
            cancellationToken);
    }

    public Task<CommandResult> SetPropertyAsync(
        string nodeId,
        string propertyName,
        string value,
        CancellationToken cancellationToken = default)
    {
        return SendUnaryAsync(
            BridgeMethod.SetProperty,
            new SetPropertyRequest
            {
                NodeId = nodeId,
                PropertyName = propertyName,
                Value = value,
            },
            CommandResult.Parser,
            cancellationToken);
    }

    public async IAsyncEnumerable<LogEntry> WatchLogsAsync(
        string minimumLevel,
        string? categoryPrefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        throw new NotSupportedException("Bridge log streaming is not implemented yet.");
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    public void Dispose()
    {
    }

    private async Task<TResponse> SendUnaryAsync<TRequest, TResponse>(
        BridgeMethod method,
        TRequest payload,
        MessageParser<TResponse> responseParser,
        CancellationToken cancellationToken)
        where TRequest : IMessage
        where TResponse : IMessage<TResponse>
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var request = new BridgeRequest
        {
            ProtocolVersion = RemoteControlProtocol.DisplayVersion,
            RequestId = requestId,
            Method = method,
            Authorization = authorization,
            Payload = payload.ToByteString(),
        };

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(endpoint.Host, endpoint.Port, cancellationToken).ConfigureAwait(false);
        await using var stream = tcpClient.GetStream();

        await BridgeFrameCodec.WriteAsync(stream, request, cancellationToken).ConfigureAwait(false);
        var response = await BridgeFrameCodec.ReadAsync(
            stream,
            BridgeResponse.Parser,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!string.Equals(response.RequestId, requestId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bridge response request ID did not match the request.");
        }

        if (!string.Equals(
            response.ProtocolVersion,
            RemoteControlProtocol.DisplayVersion,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bridge response protocol version is not supported.");
        }

        if (response.Status != BridgeStatus.Ok)
        {
            throw new InvalidOperationException(
                $"Bridge request failed with status {response.Status}: {response.ErrorMessage}");
        }

        return responseParser.ParseFrom(response.Payload);
    }
}
