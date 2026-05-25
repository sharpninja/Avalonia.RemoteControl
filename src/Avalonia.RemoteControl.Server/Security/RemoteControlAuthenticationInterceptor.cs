using System.Security.Cryptography;
using System.Text;
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
    private const string BearerPrefix = "Bearer ";
    private readonly AvaloniaRemoteControlOptions options;
    private readonly ILogger<RemoteControlAuthenticationInterceptor> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlAuthenticationInterceptor"/> class.
    /// </summary>
    /// <param name="options">Remote-control options.</param>
    /// <param name="logger">Security logger.</param>
    public RemoteControlAuthenticationInterceptor(
        IOptions<AvaloniaRemoteControlOptions> options,
        ILogger<RemoteControlAuthenticationInterceptor> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options.Value;
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
        if (!options.RequireAuthentication)
        {
            return;
        }

        var expectedToken = options.AuthenticationToken;
        var presentedToken = GetBearerToken(context.RequestHeaders);

        if (string.IsNullOrWhiteSpace(expectedToken)
            || string.IsNullOrWhiteSpace(presentedToken)
            || !FixedTimeEquals(expectedToken, presentedToken))
        {
            logger.LogWarning("Remote-control authentication rejected for {Method}", context.Method);

            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                "Authentication is required."));
        }

        context.UserState[RemoteControlClientIdentity.UserStateKey] = string.IsNullOrWhiteSpace(options.AuthenticatedClientIdentity)
            ? RemoteControlClientIdentity.Unknown
            : options.AuthenticatedClientIdentity;
    }

    private static string? GetBearerToken(global::Grpc.Core.Metadata requestHeaders)
    {
        var header = requestHeaders.Get(AuthorizationHeaderName)?.Value;

        if (header is null || !header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return header[BearerPrefix.Length..].Trim();
    }

    private static bool FixedTimeEquals(string expectedToken, string presentedToken)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
        var presentedBytes = Encoding.UTF8.GetBytes(presentedToken);

        return expectedBytes.Length == presentedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, presentedBytes);
    }
}
