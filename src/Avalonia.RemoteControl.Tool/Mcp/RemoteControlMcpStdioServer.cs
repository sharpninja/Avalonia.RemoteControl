namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Minimal MCP stdio server for exposing remote-control operations to Codex.
/// </summary>
public sealed class RemoteControlMcpStdioServer
{
    private readonly RemoteControlMcpJsonRpcHandler handler;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlMcpStdioServer"/> class.
    /// </summary>
    /// <param name="options">Remote-control MCP connection options.</param>
    /// <param name="sessionFactory">Session factory.</param>
    /// <param name="androidToolService">Optional Android MCP tool service.</param>
    public RemoteControlMcpStdioServer(
        RemoteControlMcpOptions options,
        IRemoteControlMcpSessionFactory sessionFactory,
        IAndroidMcpToolService? androidToolService = null)
        : this(() => options, sessionFactory, androidToolService)
    {
        ArgumentNullException.ThrowIfNull(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlMcpStdioServer"/> class.
    /// </summary>
    /// <param name="optionsFactory">Connection options factory.</param>
    /// <param name="sessionFactory">Session factory.</param>
    /// <param name="androidToolService">Optional Android MCP tool service.</param>
    public RemoteControlMcpStdioServer(
        Func<RemoteControlMcpOptions> optionsFactory,
        IRemoteControlMcpSessionFactory sessionFactory,
        IAndroidMcpToolService? androidToolService = null)
    {
        handler = new RemoteControlMcpJsonRpcHandler(optionsFactory, sessionFactory, androidToolService);
    }

    /// <summary>
    /// Runs the MCP stdio loop until input closes or cancellation is requested.
    /// </summary>
    /// <param name="input">Input reader.</param>
    /// <param name="output">Output writer.</param>
    /// <param name="error">Error writer used for diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code.</returns>
    public async Task<int> RunAsync(
        TextReader input,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await input.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null)
            {
                return 0;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await HandleLineAsync(line, output, error, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        handler.Dispose();
    }

    private async Task HandleLineAsync(
        string line,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(line, error, cancellationToken).ConfigureAwait(false);
        if (result.HasResponse && result.ResponseJson is not null)
        {
            await output.WriteLineAsync(result.ResponseJson.AsMemory(), cancellationToken).ConfigureAwait(false);
            await output.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
