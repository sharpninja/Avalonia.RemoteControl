using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// In-process MCP Streamable HTTP endpoint hosted by the desktop tool.
/// </summary>
public sealed class RemoteControlMcpHttpServer : IDisposable
{
    private const string RoutePrefix = "/mcp/";

    private readonly HttpListener listener = new();
    private readonly CancellationTokenSource cancellation = new();
    private readonly Func<RemoteControlMcpOptions> optionsFactory;
    private readonly IRemoteControlMcpSessionFactory sessionFactory;
    private readonly string routeSecret;
    private readonly Task listenerTask;
    private bool disposed;

    private RemoteControlMcpHttpServer(
        Uri endpoint,
        string listenerPrefix,
        string routeSecret,
        Func<RemoteControlMcpOptions> optionsFactory,
        IRemoteControlMcpSessionFactory sessionFactory)
    {
        Endpoint = endpoint;
        this.routeSecret = routeSecret;
        this.optionsFactory = optionsFactory ?? throw new ArgumentNullException(nameof(optionsFactory));
        this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        listener.Prefixes.Add(listenerPrefix);
        listener.Start();
        listenerTask = Task.Run(ListenAsync);
    }

    /// <summary>
    /// Gets the loopback URL for Codex Streamable HTTP MCP registration.
    /// </summary>
    public Uri Endpoint { get; }

    /// <summary>
    /// Starts a loopback MCP HTTP server on an available local port.
    /// </summary>
    /// <param name="optionsFactory">Factory for the current remote-control connection settings.</param>
    /// <param name="sessionFactory">Optional session factory.</param>
    /// <returns>Running MCP HTTP server.</returns>
    public static RemoteControlMcpHttpServer Start(
        Func<RemoteControlMcpOptions> optionsFactory,
        IRemoteControlMcpSessionFactory? sessionFactory = null)
    {
        var port = FindAvailablePort();
        var routeSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var listenerPrefix = $"http://127.0.0.1:{port}{RoutePrefix}";
        var endpoint = new Uri($"{listenerPrefix}{routeSecret}");
        return new RemoteControlMcpHttpServer(
            endpoint,
            listenerPrefix,
            routeSecret,
            optionsFactory,
            sessionFactory ?? new RemoteControlMcpSessionFactory());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cancellation.Cancel();
        listener.Stop();
        listener.Close();
        cancellation.Dispose();
        try
        {
            listenerTask.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
        }
    }

    private async Task ListenAsync()
    {
        while (!cancellation.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (HttpListenerException) when (cancellation.IsCancellationRequested || disposed)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            _ = Task.Run(() => HandleContextAsync(context, cancellation.Token), cancellation.Token);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsExpectedRoute(context.Request.Url))
            {
                await WriteStatusAsync(context.Response, 404, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!IsAllowedOrigin(context.Request.Headers["Origin"]))
            {
                await WriteStatusAsync(context.Response, 403, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
            {
                await WriteStatusAsync(context.Response, 405, cancellationToken).ConfigureAwait(false);
                return;
            }

            using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
            var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            using var handler = new RemoteControlMcpJsonRpcHandler(optionsFactory, sessionFactory);
            var result = await handler.HandleAsync(body, TextWriter.Null, cancellationToken).ConfigureAwait(false);

            if (!result.HasResponse || result.ResponseJson is null)
            {
                await WriteStatusAsync(context.Response, 202, cancellationToken).ConfigureAwait(false);
                return;
            }

            var bytes = Encoding.UTF8.GetBytes(result.ResponseJson);
            context.Response.StatusCode = result.HttpStatusCode;
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            var bytes = Encoding.UTF8.GetBytes(ex.Message);
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain; charset=utf-8";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Close();
        }
    }

    private bool IsExpectedRoute(Uri? uri)
    {
        if (uri is null)
        {
            return false;
        }

        var expected = RoutePrefix + routeSecret;
        return string.Equals(uri.AbsolutePath.TrimEnd('/'), expected, StringComparison.Ordinal);
    }

    private static bool IsAllowedOrigin(string? origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
        {
            return true;
        }

        return Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            (uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
             uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteStatusAsync(
        HttpListenerResponse response,
        int statusCode,
        CancellationToken cancellationToken)
    {
        response.StatusCode = statusCode;
        response.ContentLength64 = 0;
        await response.OutputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static int FindAvailablePort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }
}
