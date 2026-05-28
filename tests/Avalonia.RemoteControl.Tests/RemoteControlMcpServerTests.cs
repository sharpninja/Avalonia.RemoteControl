using System.Text.Json;
using System.Net;
using Avalonia.RemoteControl.Tool;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlMcpServerTests
{
    [Fact]
    public void McpHttpErrorResponseUsesJsonRpcApplicationJsonShape()
    {
        var response = RemoteControlMcpHttpErrorResponse.InternalServerError("boom");

        Assert.Equal(500, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.ContentType);
        Assert.NotEmpty(response.GetUtf8Bytes());
        using var body = JsonDocument.Parse(response.ResponseJson);
        Assert.Equal("2.0", body.RootElement.GetProperty("jsonrpc").GetString());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("id").ValueKind);
        var error = body.RootElement.GetProperty("error");
        Assert.Equal(-32000, error.GetProperty("code").GetInt32());
        Assert.Equal("boom", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task McpStdioServerInitializesAndListsTools()
    {
        var input = string.Join(
            Environment.NewLine,
            """
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
            """,
            """
            {"jsonrpc":"2.0","method":"notifications/initialized"}
            """,
            """
            {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
            """) + Environment.NewLine;
        var output = new StringWriter();

        var exitCode = await new RemoteControlMcpCommandLine(new FakeSessionFactory())
            .RunAsync(
                ["stdio", "--endpoint", "http://127.0.0.1:47100/", "--token", "dev-token"],
                new StringReader(input),
                output,
                TextWriter.Null);

        Assert.Equal(0, exitCode);
        var responses = ParseOutput(output);
        Assert.Equal(2, responses.Count);
        Assert.Equal("2.0", responses[0].RootElement.GetProperty("jsonrpc").GetString());
        var initializeResult = responses[0].RootElement.GetProperty("result");
        Assert.True(initializeResult.GetProperty("capabilities").TryGetProperty("tools", out _));
        Assert.Equal(RemoteControlMcpToolCatalog.ServerName, initializeResult.GetProperty("serverInfo").GetProperty("name").GetString());
        Assert.Equal(RemoteControlMcpToolCatalog.ServerTitle, initializeResult.GetProperty("serverInfo").GetProperty("title").GetString());
        var instructions = initializeResult.GetProperty("instructions").GetString();
        Assert.Contains(RemoteControlMcpToolCatalog.GetCapabilities, instructions, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.GetSnapshot, instructions, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.SetProperty, instructions, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidListDevices, instructions, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidStartAvd, instructions, StringComparison.Ordinal);
        Assert.Contains("Do not use screenshots", instructions, StringComparison.Ordinal);

        var tools = responses[1].RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToArray();
        Assert.Contains(RemoteControlMcpToolCatalog.GetCapabilities, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.GetSnapshot, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.InvokeClick, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.Focus, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.SetProperty, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidListDevices, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidListAvds, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidStartAvd, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidForward, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidLogcat, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.AndroidUiTree, tools);
    }

    [Fact]
    public async Task McpStdioServerInvokesRemoteControlTool()
    {
        var factory = new FakeSessionFactory();
        var input = """
            {"jsonrpc":"2.0","id":"call-1","method":"tools/call","params":{"name":"avalonia_remote_invoke_click","arguments":{"nodeId":"node-42"}}}

            """;
        var output = new StringWriter();

        var exitCode = await new RemoteControlMcpCommandLine(factory)
            .RunAsync(
                ["stdio", "--endpoint", "http://127.0.0.1:47100/", "--token", "dev-token", "--transport", "arc-protobuf-v1"],
                new StringReader(input),
                output,
                TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.Equal("arc-protobuf-v1", factory.Options?.TransportProtocol);
        Assert.Equal("node-42", factory.Session.ClickedNodeId);

        using var response = ParseOutput(output).Single();
        var result = response.RootElement.GetProperty("result");
        Assert.False(result.GetProperty("isError").GetBoolean());
        Assert.True(result.GetProperty("structuredContent").GetProperty("success").GetBoolean());
        Assert.Contains("clicked", result.GetProperty("content")[0].GetProperty("text").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpStdioServerReturnsJsonRpcErrorWhenToolFails()
    {
        var factory = new FakeSessionFactory();
        factory.Session.CapabilitiesException = new ObjectDisposedException("JsonDocument");
        var input = """
            {"jsonrpc":"2.0","id":"call-1","method":"tools/call","params":{"name":"avalonia_remote_get_capabilities","arguments":{}}}

            """;
        var output = new StringWriter();

        var exitCode = await new RemoteControlMcpCommandLine(factory)
            .RunAsync(
                ["stdio", "--endpoint", "http://127.0.0.1:47100/", "--token", "dev-token"],
                new StringReader(input),
                output,
                TextWriter.Null);

        Assert.Equal(0, exitCode);
        using var response = ParseOutput(output).Single();
        Assert.Equal("call-1", response.RootElement.GetProperty("id").GetString());
        var error = response.RootElement.GetProperty("error");
        Assert.Equal(-32000, error.GetProperty("code").GetInt32());
        Assert.Contains("JsonDocument", error.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpStdioServerReturnsInvalidParamsForToolArgumentFailures()
    {
        var input = """
            {"jsonrpc":"2.0","id":"call-1","method":"tools/call","params":{"name":"avalonia_remote_invoke_click","arguments":{}}}

            """;
        var output = new StringWriter();

        var exitCode = await new RemoteControlMcpCommandLine(new FakeSessionFactory())
            .RunAsync(
                ["stdio", "--endpoint", "http://127.0.0.1:47100/", "--token", "dev-token"],
                new StringReader(input),
                output,
                TextWriter.Null);

        Assert.Equal(0, exitCode);
        using var response = ParseOutput(output).Single();
        Assert.Equal("call-1", response.RootElement.GetProperty("id").GetString());
        var error = response.RootElement.GetProperty("error");
        Assert.Equal(-32602, error.GetProperty("code").GetInt32());
        Assert.Contains("nodeId", error.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpStdioServerSuppressesNotificationToolFailures()
    {
        var factory = new FakeSessionFactory();
        factory.Session.CapabilitiesException = new InvalidOperationException("remote unavailable");
        var input = """
            {"jsonrpc":"2.0","method":"tools/call","params":{"name":"avalonia_remote_get_capabilities","arguments":{}}}

            """;
        var output = new StringWriter();
        var errors = new StringWriter();

        var exitCode = await new RemoteControlMcpCommandLine(factory)
            .RunAsync(
                ["stdio", "--endpoint", "http://127.0.0.1:47100/", "--token", "dev-token"],
                new StringReader(input),
                output,
                errors);

        Assert.Equal(0, exitCode);
        Assert.Empty(ParseOutput(output));
        Assert.Contains("MCP notification failure", errors.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpCommandLineRejectsMissingToken()
    {
        var errors = new StringWriter();
        var exitCode = await new RemoteControlMcpCommandLine(new FakeSessionFactory())
            .RunAsync(["stdio"], TextReader.Null, TextWriter.Null, errors);

        Assert.Equal(2, exitCode);
        Assert.Contains("Specify --token or --token-env", errors.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpStdioServerInvokesAndroidToolWithoutRemoteSession()
    {
        var factory = new FakeSessionFactory();
        var androidTools = new FakeAndroidToolService();
        var input = """
            {"jsonrpc":"2.0","id":"android-1","method":"tools/call","params":{"name":"avalonia_android_tap","arguments":{"serial":"emulator-5554","x":20,"y":40}}}

            """;
        var output = new StringWriter();

        var exitCode = await new RemoteControlMcpCommandLine(factory, androidTools)
            .RunAsync(
                ["stdio", "--endpoint", "http://127.0.0.1:47100/", "--token", "dev-token"],
                new StringReader(input),
                output,
                TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.Null(factory.Options);
        Assert.Equal(RemoteControlMcpToolCatalog.AndroidTap, androidTools.ToolName);
        Assert.Equal("emulator-5554", androidTools.Serial);

        using var response = ParseOutput(output).Single();
        var result = response.RootElement.GetProperty("result");
        Assert.False(result.GetProperty("isError").GetBoolean());
        Assert.True(result.GetProperty("structuredContent").GetProperty("androidTool").GetBoolean());
    }

    [Fact]
    public async Task McpCommandLineDoesNotUseEnvironmentConfiguration()
    {
        var previousToken = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_TOKEN");
        var previousEndpoint = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_ENDPOINT");
        var previousTransport = Environment.GetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_TRANSPORT");
        try
        {
            Environment.SetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_TOKEN", "env-token");
            Environment.SetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_ENDPOINT", "http://127.0.0.1:49999/");
            Environment.SetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_TRANSPORT", "arc-protobuf-v1");
            var factory = new FakeSessionFactory();
            var errors = new StringWriter();

            var exitCode = await new RemoteControlMcpCommandLine(factory)
                .RunAsync(["stdio"], TextReader.Null, TextWriter.Null, errors);

            Assert.Equal(2, exitCode);
            Assert.Contains("Specify --token or --token-env", errors.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_TOKEN", previousToken);
            Environment.SetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_ENDPOINT", previousEndpoint);
            Environment.SetEnvironmentVariable("AVALONIA_REMOTE_CONTROL_TRANSPORT", previousTransport);
        }
    }

    [Fact]
    public async Task McpStreamableHttpServerInitializesAndListsTools()
    {
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), "dev-token"),
            new FakeSessionFactory());
        using var client = new HttpClient();

        var initialize = await PostJsonAsync(
            client,
            server.Endpoint,
            """
            {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
            """);
        var listTools = await PostJsonAsync(
            client,
            server.Endpoint,
            """
            {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
            """);

        Assert.Equal(HttpStatusCode.OK, initialize.StatusCode);
        Assert.Equal(HttpStatusCode.OK, listTools.StatusCode);
        using var initializeBody = JsonDocument.Parse(await initialize.Content.ReadAsStringAsync());
        var initializeResult = initializeBody.RootElement.GetProperty("result");
        Assert.True(initializeResult.GetProperty("capabilities").TryGetProperty("tools", out _));
        Assert.Contains(
            RemoteControlMcpToolCatalog.GetSnapshot,
            initializeResult.GetProperty("instructions").GetString(),
            StringComparison.Ordinal);

        using var toolsBody = JsonDocument.Parse(await listTools.Content.ReadAsStringAsync());
        var tools = toolsBody.RootElement
            .GetProperty("result")
            .GetProperty("tools")
            .EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ToArray();
        Assert.Contains(RemoteControlMcpToolCatalog.GetCapabilities, tools);
        Assert.Contains(RemoteControlMcpToolCatalog.SetProperty, tools);
    }

    [Fact]
    public async Task McpStreamableHttpServerRejectsGetAndWrongPath()
    {
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), "dev-token"),
            new FakeSessionFactory());
        using var client = new HttpClient();
        var wrongPath = new Uri(server.Endpoint.ToString() + "-wrong");

        var getResponse = await client.GetAsync(server.Endpoint);
        var wrongPathResponse = await PostJsonAsync(
            client,
            wrongPath,
            """
            {"jsonrpc":"2.0","id":1,"method":"ping","params":{}}
            """);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, wrongPathResponse.StatusCode);
    }

    [Fact]
    public async Task McpStreamableHttpServerRejectsNonLoopbackOrigin()
    {
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), "dev-token"),
            new FakeSessionFactory());
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, server.Endpoint)
        {
            Content = new StringContent(
                """
                {"jsonrpc":"2.0","id":1,"method":"ping","params":{}}
                """,
                System.Text.Encoding.UTF8,
                "application/json"),
        };
        request.Headers.TryAddWithoutValidation("Origin", "https://example.invalid");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task McpStreamableHttpServerInvokesRemoteControlTool()
    {
        var factory = new FakeSessionFactory();
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(
                new Uri("http://127.0.0.1:47100/"),
                "dev-token",
                "arc-protobuf-v1"),
            factory);
        using var client = new HttpClient();

        var response = await PostJsonAsync(
            client,
            server.Endpoint,
            """
            {"jsonrpc":"2.0","id":"call-1","method":"tools/call","params":{"name":"avalonia_remote_invoke_click","arguments":{"nodeId":"node-42"}}}
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("arc-protobuf-v1", factory.Options?.TransportProtocol);
        Assert.Equal("node-42", factory.Session.ClickedNodeId);
    }

    [Fact]
    public async Task McpStreamableHttpServerReturnsJsonRpcErrorWhenToolFails()
    {
        var factory = new FakeSessionFactory();
        factory.Session.CapabilitiesException = new ObjectDisposedException("JsonDocument");
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), "dev-token"),
            factory);
        using var client = new HttpClient();

        var response = await PostJsonAsync(
            client,
            server.Endpoint,
            """
            {"jsonrpc":"2.0","id":"call-1","method":"tools/call","params":{"name":"avalonia_remote_get_capabilities","arguments":{}}}
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.ToString(), StringComparison.Ordinal);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("call-1", body.RootElement.GetProperty("id").GetString());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal(-32000, error.GetProperty("code").GetInt32());
        Assert.Contains("JsonDocument", error.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpStreamableHttpServerReturnsInvalidParamsForToolArgumentFailures()
    {
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), "dev-token"),
            new FakeSessionFactory());
        using var client = new HttpClient();

        var response = await PostJsonAsync(
            client,
            server.Endpoint,
            """
            {"jsonrpc":"2.0","id":"call-1","method":"tools/call","params":{"name":"avalonia_remote_invoke_click","arguments":{}}}
            """);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("application/json", response.Content.Headers.ContentType?.ToString(), StringComparison.Ordinal);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("call-1", body.RootElement.GetProperty("id").GetString());
        var error = body.RootElement.GetProperty("error");
        Assert.Equal(-32602, error.GetProperty("code").GetInt32());
        Assert.Contains("nodeId", error.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task McpStreamableHttpServerSuppressesNotificationToolFailures()
    {
        var factory = new FakeSessionFactory();
        factory.Session.CapabilitiesException = new InvalidOperationException("remote unavailable");
        using var server = RemoteControlMcpHttpServer.Start(
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), "dev-token"),
            factory);
        using var client = new HttpClient();

        var response = await PostJsonAsync(
            client,
            server.Endpoint,
            """
            {"jsonrpc":"2.0","method":"tools/call","params":{"name":"avalonia_remote_get_capabilities","arguments":{}}}
            """);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength.GetValueOrDefault());
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, Uri endpoint, string json)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        };
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        return await client.SendAsync(request);
    }

    private static List<JsonDocument> ParseOutput(StringWriter output)
    {
        return output.ToString()
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line))
            .ToList();
    }

    private sealed class FakeSessionFactory : IRemoteControlMcpSessionFactory
    {
        public FakeSession Session { get; } = new();

        public RemoteControlMcpOptions? Options { get; private set; }

        public Task<IRemoteControlMcpSession> CreateAsync(
            RemoteControlMcpOptions options,
            CancellationToken cancellationToken = default)
        {
            Options = options;
            return Task.FromResult<IRemoteControlMcpSession>(Session);
        }
    }

    private sealed class FakeSession : IRemoteControlMcpSession
    {
        public Exception? CapabilitiesException { get; set; }

        public string? ClickedNodeId { get; private set; }

        public Task<string> GetCapabilitiesJsonAsync(CancellationToken cancellationToken = default)
        {
            return CapabilitiesException is null
                ? Task.FromResult("""{"protocolVersion":"1.0","supportsSnapshots":true}""")
                : Task.FromException<string>(CapabilitiesException);
        }

        public Task<string> GetSnapshotJsonAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("""{"nodes":[{"id":"root","type":"Window"}]}""");

        public Task<string> InvokeClickJsonAsync(string nodeId, CancellationToken cancellationToken = default)
        {
            ClickedNodeId = nodeId;
            return Task.FromResult("""{"success":true,"message":"clicked node-42"}""");
        }

        public Task<string> FocusJsonAsync(string nodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult("""{"success":true,"message":"focused"}""");

        public Task<string> SetPropertyJsonAsync(
            string nodeId,
            string propertyName,
            string value,
            CancellationToken cancellationToken = default) =>
            Task.FromResult("""{"success":true,"message":"property set"}""");

        public void Dispose()
        {
        }
    }

    private sealed class FakeAndroidToolService : IAndroidMcpToolService
    {
        public string? ToolName { get; private set; }

        public string? Serial { get; private set; }

        public Task<string> CallAsync(
            string toolName,
            JsonElement arguments,
            CancellationToken cancellationToken = default)
        {
            ToolName = toolName;
            Serial = arguments.GetProperty("serial").GetString();
            return Task.FromResult("""{"androidTool":true,"sent":true}""");
        }
    }
}
