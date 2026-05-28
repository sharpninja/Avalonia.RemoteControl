using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Command-line entry point for the tool-side MCP server.
/// </summary>
public sealed class RemoteControlMcpCommandLine
{
    private readonly IRemoteControlMcpSessionFactory sessionFactory;
    private readonly IAndroidMcpToolService? androidToolService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlMcpCommandLine"/> class.
    /// </summary>
    /// <param name="sessionFactory">Session factory.</param>
    /// <param name="androidToolService">Optional Android MCP tool service.</param>
    public RemoteControlMcpCommandLine(
        IRemoteControlMcpSessionFactory? sessionFactory = null,
        IAndroidMcpToolService? androidToolService = null)
    {
        this.sessionFactory = sessionFactory ?? new RemoteControlMcpSessionFactory();
        this.androidToolService = androidToolService;
    }

    /// <summary>
    /// Runs the MCP command line.
    /// </summary>
    /// <param name="args">Command-line arguments after <c>mcp</c>.</param>
    /// <param name="input">Input reader.</param>
    /// <param name="output">Output writer.</param>
    /// <param name="error">Error writer.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exit code.</returns>
    public async Task<int> RunAsync(
        string[] args,
        TextReader input,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            await error.WriteLineAsync(CreateHelpText()).ConfigureAwait(false);
            return 0;
        }

        var effectiveArgs = args.Length == 0 ? ["stdio"] : args;
        if (!effectiveArgs[0].Equals("stdio", StringComparison.OrdinalIgnoreCase))
        {
            await error.WriteLineAsync("Unsupported MCP transport. Use 'avalonia-remote mcp stdio'.").ConfigureAwait(false);
            return 2;
        }

        if (!TryParseOptions(effectiveArgs[1..], out var options, out var failure))
        {
            await error.WriteLineAsync(failure).ConfigureAwait(false);
            return 2;
        }

        var server = new RemoteControlMcpStdioServer(options!, sessionFactory, androidToolService);
        try
        {
            return await server.RunAsync(input, output, error, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            server.Dispose();
        }
    }

    /// <summary>
    /// Creates help text for the MCP command.
    /// </summary>
    /// <returns>Help text.</returns>
    public static string CreateHelpText() =>
        """
        Usage:
          avalonia-remote mcp [stdio] [--endpoint <uri>] [--token <token> | --token-env <env-var>] [--transport <protocol>] [--certificate <path>] [--accepted-fingerprint <sha256>]

        Starts the diagnostic MCP stdio server exposing approved Avalonia.RemoteControl operations to Codex.
        The desktop app hosts the default self-contained MCP endpoint in-process over loopback Streamable HTTP.
        Defaults:
          endpoint: http://127.0.0.1:47100/
          transport: grpc
        """;

    private static bool TryParseOptions(
        string[] args,
        out RemoteControlMcpOptions? options,
        out string failure)
    {
        options = null;
        failure = string.Empty;
        string? endpoint = RemoteControlMcpOptions.DefaultEndpoint.ToString();
        string? token = null;
        string? tokenEnvironmentVariable = null;
        var transport = RemoteControlProtocol.GrpcTransportProtocol;
        string? certificate = null;
        string? acceptedFingerprint = null;

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--endpoint":
                    endpoint = ReadValue(args, ref index, arg, out failure);
                    if (failure.Length > 0) return false;
                    break;
                case "--token":
                    token = ReadValue(args, ref index, arg, out failure);
                    if (failure.Length > 0) return false;
                    break;
                case "--token-env":
                    tokenEnvironmentVariable = ReadValue(args, ref index, arg, out failure);
                    if (failure.Length > 0) return false;
                    break;
                case "--transport":
                    transport = ReadValue(args, ref index, arg, out failure) ?? transport;
                    if (failure.Length > 0) return false;
                    break;
                case "--certificate":
                    certificate = ReadValue(args, ref index, arg, out failure);
                    if (failure.Length > 0) return false;
                    break;
                case "--accepted-fingerprint":
                    acceptedFingerprint = ReadValue(args, ref index, arg, out failure);
                    if (failure.Length > 0) return false;
                    break;
                default:
                    failure = $"Unsupported MCP option '{arg}'.";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri))
        {
            failure = "--endpoint must be an absolute URI.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(tokenEnvironmentVariable))
        {
            token = Environment.GetEnvironmentVariable(tokenEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(token))
            {
                failure = $"Token environment variable '{tokenEnvironmentVariable}' is not set.";
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            failure = "Specify --token or --token-env.";
            return false;
        }

        options = RemoteControlMcpOptions.Create(
            endpointUri,
            token,
            transport,
            certificate,
            acceptedFingerprint);
        return true;
    }

    private static string? ReadValue(string[] args, ref int index, string option, out string failure)
    {
        if (index + 1 >= args.Length)
        {
            failure = $"{option} requires a value.";
            return null;
        }

        failure = string.Empty;
        index++;
        return args[index];
    }
}
