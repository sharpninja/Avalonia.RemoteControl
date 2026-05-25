using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Server.Security;

/// <summary>
/// Enforces bearer authentication on remote-control gRPC calls.
/// </summary>
public sealed class RemoteControlAuthenticationInterceptor : Interceptor
{
    private const string AuthorizationHeaderName = "authorization";
    private readonly RemoteControlBearerTokenAuthenticator authenticator;
    private readonly ILogger<RemoteControlAuthenticationInterceptor> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlAuthenticationInterceptor"/> class.
    /// </summary>
    /// <param name="options">Remote-control options.</param>
    /// <param name="logger">Security logger.</param>
    public RemoteControlAuthenticationInterceptor(
        IOptions<AvaloniaRemoteControlOptions> options,
        ILogger<RemoteControlAuthenticationInterceptor> logger)
        : this(new RemoteControlBearerTokenAuthenticator(options), logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlAuthenticationInterceptor"/> class.
    /// </summary>
    /// <param name="authenticator">Transport-independent bearer token authenticator.</param>
    /// <param name="logger">Security logger.</param>
    public RemoteControlAuthenticationInterceptor(
        RemoteControlBearerTokenAuthenticator authenticator,
        ILogger<RemoteControlAuthenticationInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(authenticator);

        this.authenticator = authenticator;
        this.logger = logger;
    }

    /// <inheritdoc />
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthenticated(context);
        return await continuation(request, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthenticated(context);
        return await continuation(requestStream, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthenticated(context);
        await continuation(request, responseStream, context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        EnsureAuthenticated(context);
        await continuation(requestStream, responseStream, context).ConfigureAwait(false);
    }

    private void EnsureAuthenticated(ServerCallContext context)
    {
        var result = authenticator.AuthenticateAuthorization(GetAuthorizationHeader(context.RequestHeaders));
        if (!result.IsAuthenticated)
        {
            logger.LogWarning("Remote-control authentication rejected for {Method}", context.Method);

            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                result.FailureMessage));
        }

        context.UserState[RemoteControlClientIdentity.UserStateKey] = result.ClientIdentity;
    }

    private static string? GetAuthorizationHeader(global::Grpc.Core.Metadata requestHeaders)
    {
        return requestHeaders.Get(AuthorizationHeaderName)?.Value;
    }
}
