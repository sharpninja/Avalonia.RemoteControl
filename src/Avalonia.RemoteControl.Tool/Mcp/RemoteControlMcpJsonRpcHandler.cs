using System.Text.Json;

namespace Avalonia.RemoteControl.Tool;

internal sealed class RemoteControlMcpJsonRpcHandler : IDisposable
{
    private const string ProtocolVersion = "2025-06-18";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Func<RemoteControlMcpOptions> optionsFactory;
    private readonly IRemoteControlMcpSessionFactory sessionFactory;
    private readonly IAndroidMcpToolService androidToolService;
    private IRemoteControlMcpSession? session;

    public RemoteControlMcpJsonRpcHandler(
        Func<RemoteControlMcpOptions> optionsFactory,
        IRemoteControlMcpSessionFactory sessionFactory,
        IAndroidMcpToolService? androidToolService = null)
    {
        this.optionsFactory = optionsFactory ?? throw new ArgumentNullException(nameof(optionsFactory));
        this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        this.androidToolService = androidToolService ?? new RemoteControlAndroidMcpToolService();
    }

    public async Task<RemoteControlMcpJsonRpcResult> HandleAsync(
        string message,
        TextWriter diagnostics,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(diagnostics);

        object? id = null;
        var hasId = false;
        try
        {
            using var document = JsonDocument.Parse(message);
            var root = document.RootElement;
            if (!root.TryGetProperty("jsonrpc", out var jsonRpc)
                || jsonRpc.GetString() != "2.0"
                || !root.TryGetProperty("method", out var methodElement)
                || methodElement.ValueKind != JsonValueKind.String)
            {
                return RemoteControlMcpJsonRpcResult.Response(
                    Serialize(CreateError(null, -32600, "Invalid JSON-RPC request.")),
                    httpStatusCode: 400);
            }

            if (root.TryGetProperty("id", out var idElement))
            {
                id = Clone(idElement);
                hasId = true;
            }

            var method = methodElement.GetString()!;
            var response = await DispatchAsync(method, root, id, hasId, cancellationToken).ConfigureAwait(false);
            return response is null
                ? RemoteControlMcpJsonRpcResult.Accepted()
                : RemoteControlMcpJsonRpcResult.Response(Serialize(response));
        }
        catch (JsonException ex)
        {
            await diagnostics.WriteLineAsync($"Invalid MCP JSON message: {ex.Message}").ConfigureAwait(false);
            return RemoteControlMcpJsonRpcResult.Response(
                Serialize(CreateError(null, -32700, "Parse error.")),
                httpStatusCode: 400);
        }
        catch (ArgumentException ex) when (hasId)
        {
            return RemoteControlMcpJsonRpcResult.Response(Serialize(CreateError(id, -32602, ex.Message)));
        }
        catch (Exception ex) when (hasId && ex is not OperationCanceledException)
        {
            await diagnostics.WriteLineAsync($"MCP tool failure: {ex.Message}").ConfigureAwait(false);
            return RemoteControlMcpJsonRpcResult.Response(Serialize(CreateError(id, -32000, ex.Message)));
        }
        catch (Exception ex) when (!hasId && ex is not OperationCanceledException)
        {
            await diagnostics.WriteLineAsync($"MCP notification failure: {ex.Message}").ConfigureAwait(false);
            return RemoteControlMcpJsonRpcResult.Accepted();
        }
    }

    public void Dispose()
    {
        session?.Dispose();
        session = null;
    }

    private async Task<object?> DispatchAsync(
        string method,
        JsonElement root,
        object? id,
        bool hasId,
        CancellationToken cancellationToken)
    {
        return method switch
        {
            "initialize" => CreateResponse(
                id,
                new
                {
                    protocolVersion = GetRequestedProtocolVersion(root),
                    capabilities = new
                    {
                        tools = new
                        {
                            listChanged = false,
                        },
                    },
                    serverInfo = new
                    {
                        name = RemoteControlMcpToolCatalog.ServerName,
                        title = RemoteControlMcpToolCatalog.ServerTitle,
                        version = typeof(RemoteControlMcpJsonRpcHandler).Assembly.GetName().Version?.ToString() ?? "0.0.0",
                    },
                    instructions = RemoteControlMcpToolCatalog.CreateInitializeInstructions(),
                }),
            "notifications/initialized" => null,
            "ping" => CreateResponse(id, new { }),
            "tools/list" => CreateResponse(
                id,
                new
                {
                    tools = RemoteControlMcpToolCatalog.CreateDefinitions(),
                }),
            "tools/call" => CreateResponse(id, await CallToolAsync(root, cancellationToken).ConfigureAwait(false)),
            _ when hasId => CreateError(id, -32601, $"Method '{method}' is not supported."),
            _ => null,
        };
    }

    private async Task<object> CallToolAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (!root.TryGetProperty("params", out var parameters)
            || !parameters.TryGetProperty("name", out var nameElement)
            || nameElement.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException("Tool call params.name is required.");
        }

        using var emptyArgumentsDocument = parameters.TryGetProperty("arguments", out var argumentElement)
            && argumentElement.ValueKind == JsonValueKind.Object
                ? null
                : JsonDocument.Parse("{}");
        var arguments = emptyArgumentsDocument?.RootElement ?? argumentElement;

        var toolName = nameElement.GetString();
        if (RemoteControlMcpToolCatalog.IsAndroidTool(toolName))
        {
            return CreateToolResult(await androidToolService.CallAsync(
                toolName!,
                arguments,
                cancellationToken).ConfigureAwait(false));
        }

        var activeSession = await GetSessionAsync(cancellationToken).ConfigureAwait(false);
        var payload = toolName switch
        {
            RemoteControlMcpToolCatalog.GetCapabilities =>
                await activeSession.GetCapabilitiesJsonAsync(cancellationToken).ConfigureAwait(false),
            RemoteControlMcpToolCatalog.GetSnapshot =>
                await activeSession.GetSnapshotJsonAsync(cancellationToken).ConfigureAwait(false),
            RemoteControlMcpToolCatalog.InvokeClick =>
                await activeSession.InvokeClickJsonAsync(
                    RemoteControlMcpToolCatalog.GetRequiredString(arguments, "nodeId"),
                    cancellationToken).ConfigureAwait(false),
            RemoteControlMcpToolCatalog.Focus =>
                await activeSession.FocusJsonAsync(
                    RemoteControlMcpToolCatalog.GetRequiredString(arguments, "nodeId"),
                    cancellationToken).ConfigureAwait(false),
            RemoteControlMcpToolCatalog.SetProperty =>
                await activeSession.SetPropertyJsonAsync(
                    RemoteControlMcpToolCatalog.GetRequiredString(arguments, "nodeId"),
                    RemoteControlMcpToolCatalog.GetRequiredString(arguments, "propertyName"),
                    RemoteControlMcpToolCatalog.GetRequiredString(arguments, "value"),
                    cancellationToken).ConfigureAwait(false),
            var unknown => throw new ArgumentException($"Unknown tool: {unknown}"),
        };

        return CreateToolResult(payload);
    }

    private async Task<IRemoteControlMcpSession> GetSessionAsync(CancellationToken cancellationToken)
    {
        session ??= await sessionFactory.CreateAsync(optionsFactory(), cancellationToken).ConfigureAwait(false);
        return session;
    }

    private static object CreateToolResult(string json)
    {
        using var document = JsonDocument.Parse(json);
        return new
        {
            content = new[]
            {
                new
                {
                    type = "text",
                    text = json,
                },
            },
            structuredContent = document.RootElement.Clone(),
            isError = false,
        };
    }

    private static string GetRequestedProtocolVersion(JsonElement root)
    {
        if (root.TryGetProperty("params", out var parameters)
            && parameters.TryGetProperty("protocolVersion", out var protocolVersion)
            && protocolVersion.ValueKind == JsonValueKind.String
            && !string.IsNullOrWhiteSpace(protocolVersion.GetString()))
        {
            return protocolVersion.GetString()!;
        }

        return ProtocolVersion;
    }

    private static object CreateResponse(object? id, object result) =>
        new
        {
            jsonrpc = "2.0",
            id,
            result,
        };

    private static object CreateError(object? id, int code, string message) =>
        new
        {
            jsonrpc = "2.0",
            id,
            error = new
            {
                code,
                message,
            },
        };

    private static string Serialize(object message) => JsonSerializer.Serialize(message, JsonOptions);

    private static object? Clone(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var value) => value,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.Clone(),
        };
    }
}

internal sealed record RemoteControlMcpJsonRpcResult(
    bool HasResponse,
    string? ResponseJson,
    int HttpStatusCode)
{
    public static RemoteControlMcpJsonRpcResult Response(string responseJson, int httpStatusCode = 200) =>
        new(true, responseJson, httpStatusCode);

    public static RemoteControlMcpJsonRpcResult Accepted() => new(false, null, 202);
}
