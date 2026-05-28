namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Minimal lifecycle contract for an MCP endpoint hosted by the desktop tool.
/// </summary>
public interface IRemoteControlMcpEndpointHost : IDisposable
{
    /// <summary>
    /// Gets the MCP endpoint advertised to Codex.
    /// </summary>
    Uri Endpoint { get; }
}

/// <summary>
/// Adapter for the in-process MCP Streamable HTTP server.
/// </summary>
public sealed class RemoteControlMcpHttpEndpointHost : IRemoteControlMcpEndpointHost
{
    private readonly RemoteControlMcpHttpServer server;

    private RemoteControlMcpHttpEndpointHost(RemoteControlMcpHttpServer server)
    {
        this.server = server;
    }

    /// <inheritdoc />
    public Uri Endpoint => server.Endpoint;

    /// <summary>
    /// Starts a new in-process MCP HTTP endpoint.
    /// </summary>
    /// <param name="optionsFactory">Current remote-control options factory.</param>
    /// <returns>Endpoint host.</returns>
    public static RemoteControlMcpHttpEndpointHost Start(Func<RemoteControlMcpOptions> optionsFactory)
    {
        return new RemoteControlMcpHttpEndpointHost(RemoteControlMcpHttpServer.Start(optionsFactory));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        server.Dispose();
    }
}

/// <summary>
/// Coordinates the desktop tool MCP host and the terminal preset state shown to the user.
/// </summary>
public sealed class RemoteControlMcpHostController : IDisposable
{
    private readonly TerminalPanelViewModel terminal;
    private readonly Func<RemoteControlMcpOptions> optionsFactory;
    private readonly Func<Func<RemoteControlMcpOptions>, IRemoteControlMcpEndpointHost> hostFactory;
    private IRemoteControlMcpEndpointHost? host;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlMcpHostController"/> class.
    /// </summary>
    /// <param name="terminal">Terminal view model to seed with the MCP URL.</param>
    /// <param name="optionsFactory">Current remote-control options factory.</param>
    /// <param name="hostFactory">Optional endpoint host factory for tests.</param>
    public RemoteControlMcpHostController(
        TerminalPanelViewModel terminal,
        Func<RemoteControlMcpOptions> optionsFactory,
        Func<Func<RemoteControlMcpOptions>, IRemoteControlMcpEndpointHost>? hostFactory = null)
    {
        this.terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        this.optionsFactory = optionsFactory ?? throw new ArgumentNullException(nameof(optionsFactory));
        this.hostFactory = hostFactory ?? RemoteControlMcpHttpEndpointHost.Start;
    }

    /// <summary>
    /// Gets a value indicating whether the MCP host is running.
    /// </summary>
    public bool IsRunning => host is not null;

    /// <summary>
    /// Gets the current MCP endpoint.
    /// </summary>
    public Uri? Endpoint => host?.Endpoint;

    /// <summary>
    /// Starts the MCP endpoint if it is not already running.
    /// </summary>
    public void Start()
    {
        if (host is not null)
        {
            return;
        }

        host = hostFactory(optionsFactory);
        terminal.RemoteControlMcpUrl = host.Endpoint.ToString();
    }

    /// <summary>
    /// Restarts the MCP endpoint and updates the terminal preset URL.
    /// </summary>
    public void Restart()
    {
        Stop(clearTerminalUrl: false);
        Start();
    }

    /// <summary>
    /// Stops the MCP endpoint.
    /// </summary>
    public void Stop()
    {
        Stop(clearTerminalUrl: true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    private void Stop(bool clearTerminalUrl)
    {
        host?.Dispose();
        host = null;
        if (clearTerminalUrl)
        {
            terminal.RemoteControlMcpUrl = string.Empty;
        }
    }
}
