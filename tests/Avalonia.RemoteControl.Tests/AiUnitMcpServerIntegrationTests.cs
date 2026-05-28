using System.Buffers.Binary;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia.RemoteControl.Client.Live;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Tool;
using SharpNinja.AiUnit.Frontier;
using SharpNinja.AiUnit.Validation;
using SharpNinja.AiUnit.Xunit;
using ProtocolRect = Avalonia.RemoteControl.Protocol.V1.Rect;

namespace Avalonia.RemoteControl.Tests;

public sealed class AiUnitMcpServerIntegrationTests
{
    private const string ScenarioId = "remote-tool-mcp-wireframe-screenshot";
    private const string SchemaVersion = "arc.aiunit.remoteToolReview.v1";

    [Fact]
    public async Task RemoteToolEvidenceCollectorExercisesRunningToolMcpHost()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var evidence = await RemoteToolEvidence.CollectFromRunningToolMcpAsync(timeout.Token);

        Assert.StartsWith("http://127.0.0.1:", evidence.Mcp.Endpoint, StringComparison.Ordinal);
        Assert.Contains("/mcp/", evidence.Mcp.Endpoint, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, evidence.Mcp.InitializeStatusCode);
        Assert.Equal(HttpStatusCode.OK, evidence.Mcp.ToolsListStatusCode);
        Assert.Equal(HttpStatusCode.OK, evidence.Mcp.CapabilitiesStatusCode);
        Assert.Equal(HttpStatusCode.OK, evidence.Mcp.SnapshotStatusCode);
        Assert.Equal(HttpStatusCode.OK, evidence.Mcp.ClickStatusCode);
        Assert.True(evidence.Mcp.InstructionsUseTreeFirst);
        Assert.Equal("arc-protobuf-v1", evidence.Mcp.TransportProtocol);
        Assert.Equal("save-button", evidence.Mcp.ClickedNodeId);
        Assert.Contains(RemoteControlMcpToolCatalog.GetSnapshot, evidence.Mcp.ToolNames);
        Assert.Contains(RemoteControlMcpToolCatalog.InvokeClick, evidence.Mcp.ToolNames);
        Assert.Contains("\"absoluteBounds\"", evidence.Mcp.SnapshotJson, StringComparison.Ordinal);
    }

    [Fact]
    public void RemoteToolVisualEvidenceUsesTreeModelForWireframeHitTargets()
    {
        var evidence = RemoteToolEvidence.Sample();

        Assert.Equal(3, evidence.Visual.WireframeNodes.Count);
        Assert.Equal("save-button", evidence.Visual.PrimaryInteractionNodeId);
        Assert.Contains(evidence.Visual.WireframeNodes, node => node.Id == "save-button" && node.TypeName == "Button");
        Assert.Contains("root-relative DIPs", evidence.Visual.InteractionExpectations, StringComparer.Ordinal);
    }

    [Fact]
    public void AiUnitRemoteToolReviewPromptNamesMcpAndTreeFirstVisualComparison()
    {
        var evidence = RemoteToolEvidence.Sample();
        var systemPrompt = AiUnitRemoteToolReviewPrompt.BuildSystemPrompt();
        var userPrompt = AiUnitRemoteToolReviewPrompt.BuildUserPrompt(evidence);

        Assert.Contains(SchemaVersion, systemPrompt, StringComparison.Ordinal);
        Assert.Contains(ScenarioId, systemPrompt, StringComparison.Ordinal);
        Assert.Contains("first image is the wireframe baseline", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("second image is the screenshot", systemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(RemoteControlMcpToolCatalog.GetSnapshot, userPrompt, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.InvokeClick, userPrompt, StringComparison.Ordinal);
        Assert.Contains("Do not use screenshots or pixel inspection as the primary way to choose controls", userPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void AiUnitRemoteToolReviewPromptBuildsWireframeAndScreenshotAttachments()
    {
        var attachments = AiUnitRemoteToolReviewPrompt.BuildAttachments(RemoteToolEvidence.Sample().Visual);

        Assert.Equal(2, attachments.Count);
        Assert.Equal("image/png", attachments[0].MediaType);
        Assert.Equal("arc-live-view-wireframe.png", attachments[0].Name);
        Assert.Equal((360, 640), PngProbe.ReadDimensions(attachments[0].Data));
        Assert.Equal("image/png", attachments[1].MediaType);
        Assert.Equal("arc-live-view-screenshot.png", attachments[1].Name);
        Assert.Equal((360, 640), PngProbe.ReadDimensions(attachments[1].Data));
    }

    [Fact]
    public void AiUnitStrategyConfigUsesInstalledCodexCli()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.aiunit.json");

        Assert.True(File.Exists(configPath), $"aiUnit config was not copied to the test output directory: {configPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(configPath));
        var aiUnit = document.RootElement.GetProperty("AiUnit");
        Assert.Equal("codex-subscription", aiUnit.GetProperty("ActiveStrategy").GetString());

        var strategy = aiUnit
            .GetProperty("Strategies")
            .GetProperty("codex-subscription");
        Assert.Equal("cli", strategy.GetProperty("Kind").GetString());
        Assert.Equal("codex", strategy.GetProperty("Command").GetString());
        Assert.Equal("(cli-managed)", strategy.GetProperty("Model").GetString());
    }

    [Fact]
    public void AiUnitRemoteToolReviewValidatorAcceptsPassingJson()
    {
        const string json = """
            {
              "schemaVersion": "arc.aiunit.remoteToolReview.v1",
              "scenarioId": "remote-tool-mcp-wireframe-screenshot",
              "status": "pass",
              "summary": "The running tool MCP endpoint and live-view visual evidence satisfy the contract.",
              "mcpInteraction": {
                "status": "pass",
                "evidence": "initialize, tools/list, get_snapshot, and invoke_click all succeeded.",
                "toolsUsed": [
                  "avalonia_remote_get_snapshot",
                  "avalonia_remote_invoke_click"
                ]
              },
              "wireframeVsScreenshot": {
                "status": "pass",
                "treeFirst": true,
                "visualMatch": "match",
                "evidence": "The tree wireframe contains the actionable controls and the screenshot visually matches the same root layout.",
                "findings": []
              },
              "findings": [],
              "recommendedTests": [
                "Run against a live Android-hosted Avalonia app with the embedded MCP server enabled."
              ]
            }
            """;

        var result = AiUnitRemoteToolReviewValidator.ParseAndValidate(json, ScenarioId);

        Assert.Equal("pass", result.Status);
        Assert.Empty(result.Findings);
        Assert.Equal("pass", result.McpInteractionStatus);
        Assert.Equal("pass", result.WireframeVsScreenshotStatus);
    }

    [Fact]
    public void AiUnitRemoteToolReviewValidatorRejectsMissingVisualSection()
    {
        const string json = """
            {
              "schemaVersion": "arc.aiunit.remoteToolReview.v1",
              "scenarioId": "remote-tool-mcp-wireframe-screenshot",
              "status": "pass",
              "summary": "Missing required visual review section.",
              "mcpInteraction": {
                "status": "pass",
                "evidence": "MCP worked.",
                "toolsUsed": []
              },
              "findings": [],
              "recommendedTests": []
            }
            """;

        var failure = Assert.Throws<AiResponseValidationException>(() =>
            AiUnitRemoteToolReviewValidator.ParseAndValidate(json, ScenarioId));

        Assert.Contains("wireframeVsScreenshot", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AiUnitRemoteToolLiveReviewRunsWhenExplicitlyEnabled()
    {
        if (!RemoteToolAiUnitOptions.Enabled)
        {
            return;
        }

        var fixture = AiStrategyFixture.Default;
        Assert.True(fixture.IsResolved, fixture.SkipReason);

        using var timeout = new CancellationTokenSource(RemoteToolAiUnitOptions.Timeout);
        var evidence = await RemoteToolEvidence.CollectFromRunningToolMcpAsync(timeout.Token);
        var request = new FrontierRequest(
            SystemPrompt: AiUnitRemoteToolReviewPrompt.BuildSystemPrompt(),
            UserMessage: AiUnitRemoteToolReviewPrompt.BuildUserPrompt(evidence),
            Attachments: AiUnitRemoteToolReviewPrompt.BuildAttachments(evidence.Visual),
            Temperature: 0.0,
            RequireJsonOutput: true);

        var response = await fixture.Client!.SendAsync(request, timeout.Token);
        if (response.Error is { } error)
        {
            Assert.Fail($"{error.ErrorCode}: {error.Message}");
        }

        var result = AiUnitRemoteToolReviewValidator.ParseAndValidate(
            response.Text ?? string.Empty,
            ScenarioId);
        Assert.NotEqual("error", result.Status);

        var blocking = result.Findings
            .Where(static finding => finding.Severity is "critical" or "high")
            .Select(static finding => $"{finding.Severity}: {finding.Title} - {finding.Recommendation}")
            .ToArray();

        Assert.Empty(blocking);
    }

    private static class RemoteToolAiUnitOptions
    {
        public static bool Enabled =>
            IsTrue(Environment.GetEnvironmentVariable("ARC_AIUNIT_REMOTE_TOOL_TESTS_ENABLED"))
            || IsTrue(Environment.GetEnvironmentVariable("ARC_AIUNIT_MCP_SERVER_TESTS_ENABLED"));

        public static TimeSpan Timeout =>
            int.TryParse(Environment.GetEnvironmentVariable("ARC_AIUNIT_REMOTE_TOOL_TIMEOUT_SECONDS"), out var seconds)
            && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.FromMinutes(10);

        private static bool IsTrue(string? value) =>
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }

    private sealed record RemoteToolEvidence(
        string ScenarioId,
        RemoteToolMcpEvidence Mcp,
        RemoteToolVisualEvidence Visual,
        IReadOnlyList<string> ReviewGuidance)
    {
        public static RemoteToolEvidence Sample()
        {
            var visual = RemoteToolVisualEvidence.FromSnapshot(CreateSampleSnapshot());
            return new RemoteToolEvidence(
                AiUnitMcpServerIntegrationTests.ScenarioId,
                new RemoteToolMcpEvidence(
                    Endpoint: "http://127.0.0.1:49152/mcp/sample-secret",
                    ServerName: RemoteControlMcpToolCatalog.ServerName,
                    ServerTitle: RemoteControlMcpToolCatalog.ServerTitle,
                    InitializeStatusCode: HttpStatusCode.OK,
                    ToolsListStatusCode: HttpStatusCode.OK,
                    CapabilitiesStatusCode: HttpStatusCode.OK,
                    SnapshotStatusCode: HttpStatusCode.OK,
                    ClickStatusCode: HttpStatusCode.OK,
                    InstructionsUseTreeFirst: true,
                    TransportProtocol: "arc-protobuf-v1",
                    ToolNames:
                    [
                        RemoteControlMcpToolCatalog.GetCapabilities,
                        RemoteControlMcpToolCatalog.GetSnapshot,
                        RemoteControlMcpToolCatalog.InvokeClick,
                    ],
                    CapabilitiesJson: FakeSession.CapabilitiesJson,
                    SnapshotJson: FakeSession.SnapshotJson,
                    ClickedNodeId: visual.PrimaryInteractionNodeId,
                    ClickResultJson: """{"success":true,"message":"clicked save-button"}"""),
                visual,
                [
                    "Use the MCP control tree and absoluteBounds as the authoritative interaction model.",
                    "Use screenshots only to compare visual fidelity after controls are identified from tree data.",
                    "The live-view wireframe must use root-relative DIPs and current node IDs.",
                ]);
        }

        public static async Task<RemoteToolEvidence> CollectFromRunningToolMcpAsync(CancellationToken cancellationToken)
        {
            var factory = new FakeSessionFactory();
            using var server = RemoteControlMcpHttpServer.Start(
                () => RemoteControlMcpOptions.Create(
                    new Uri("http://127.0.0.1:47100/"),
                    "dev-token",
                    "arc-protobuf-v1"),
                factory);
            using var client = new HttpClient();

            var initialize = await PostJsonAsync(
                client,
                server.Endpoint,
                """
                {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"aiunit","version":"1.0"}}}
                """,
                cancellationToken);
            var toolsList = await PostJsonAsync(
                client,
                server.Endpoint,
                """
                {"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}
                """,
                cancellationToken);
            var capabilities = await PostJsonAsync(
                client,
                server.Endpoint,
                """
                {"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"avalonia_remote_get_capabilities","arguments":{}}}
                """,
                cancellationToken);
            var snapshot = await PostJsonAsync(
                client,
                server.Endpoint,
                """
                {"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"avalonia_remote_get_snapshot","arguments":{}}}
                """,
                cancellationToken);
            var click = await PostJsonAsync(
                client,
                server.Endpoint,
                """
                {"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"avalonia_remote_invoke_click","arguments":{"nodeId":"save-button"}}}
                """,
                cancellationToken);

            using var initializeJson = JsonDocument.Parse(await initialize.Content.ReadAsStringAsync(cancellationToken));
            using var toolsJson = JsonDocument.Parse(await toolsList.Content.ReadAsStringAsync(cancellationToken));
            using var capabilitiesJson = JsonDocument.Parse(await capabilities.Content.ReadAsStringAsync(cancellationToken));
            using var snapshotJson = JsonDocument.Parse(await snapshot.Content.ReadAsStringAsync(cancellationToken));
            using var clickJson = JsonDocument.Parse(await click.Content.ReadAsStringAsync(cancellationToken));

            var initializeResult = initializeJson.RootElement.GetProperty("result");
            var instructions = initializeResult.GetProperty("instructions").GetString() ?? string.Empty;
            var toolNames = toolsJson.RootElement
                .GetProperty("result")
                .GetProperty("tools")
                .EnumerateArray()
                .Select(static tool => tool.GetProperty("name").GetString() ?? string.Empty)
                .Where(static tool => tool.Length > 0)
                .ToArray();
            var visual = RemoteToolVisualEvidence.FromSnapshot(CreateSampleSnapshot());

            return new RemoteToolEvidence(
                AiUnitMcpServerIntegrationTests.ScenarioId,
                new RemoteToolMcpEvidence(
                    Endpoint: server.Endpoint.ToString(),
                    ServerName: initializeResult.GetProperty("serverInfo").GetProperty("name").GetString() ?? string.Empty,
                    ServerTitle: initializeResult.GetProperty("serverInfo").GetProperty("title").GetString() ?? string.Empty,
                    InitializeStatusCode: initialize.StatusCode,
                    ToolsListStatusCode: toolsList.StatusCode,
                    CapabilitiesStatusCode: capabilities.StatusCode,
                    SnapshotStatusCode: snapshot.StatusCode,
                    ClickStatusCode: click.StatusCode,
                    InstructionsUseTreeFirst: instructions.Contains("Do not use screenshots", StringComparison.Ordinal)
                        && instructions.Contains(RemoteControlMcpToolCatalog.GetSnapshot, StringComparison.Ordinal),
                    TransportProtocol: factory.Options?.TransportProtocol ?? string.Empty,
                    ToolNames: toolNames,
                    CapabilitiesJson: GetToolText(capabilitiesJson.RootElement),
                    SnapshotJson: GetToolText(snapshotJson.RootElement),
                    ClickedNodeId: factory.Session.ClickedNodeId ?? string.Empty,
                    ClickResultJson: GetToolText(clickJson.RootElement)),
                visual,
                [
                    "Use the running avalonia-remote MCP HTTP endpoint as the integration boundary.",
                    "Use tree data from avalonia_remote_get_snapshot before selecting controls.",
                    "Compare the live-view wireframe with the screenshot only after the tree has identified controls and bounds.",
                ]);
        }

        private static async Task<HttpResponseMessage> PostJsonAsync(
            HttpClient client,
            Uri endpoint,
            string json,
            CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.Accept.ParseAdd("text/event-stream");
            return await client.SendAsync(request, cancellationToken);
        }

        private static string GetToolText(JsonElement responseRoot)
        {
            return responseRoot
                .GetProperty("result")
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
    }

    private sealed record RemoteToolMcpEvidence(
        string Endpoint,
        string ServerName,
        string ServerTitle,
        HttpStatusCode InitializeStatusCode,
        HttpStatusCode ToolsListStatusCode,
        HttpStatusCode CapabilitiesStatusCode,
        HttpStatusCode SnapshotStatusCode,
        HttpStatusCode ClickStatusCode,
        bool InstructionsUseTreeFirst,
        string TransportProtocol,
        IReadOnlyList<string> ToolNames,
        string CapabilitiesJson,
        string SnapshotJson,
        string ClickedNodeId,
        string ClickResultJson);

    private sealed record RemoteToolVisualEvidence(
        string ScreenId,
        string WireframeAttachmentName,
        string ScreenshotAttachmentName,
        int Width,
        int Height,
        string PrimaryInteractionNodeId,
        IReadOnlyList<WireframeNodeEvidence> WireframeNodes,
        IReadOnlyList<string> InteractionExpectations)
    {
        public static RemoteToolVisualEvidence FromSnapshot(TreeSnapshot snapshot)
        {
            var model = new RemoteLiveTreeModel();
            model.ApplySnapshot(snapshot);
            var primaryHit = model.HitTest(240, 445);
            model.SelectNode(primaryHit?.Id);

            return new RemoteToolVisualEvidence(
                "live-view-settings-panel",
                "arc-live-view-wireframe.png",
                "arc-live-view-screenshot.png",
                360,
                640,
                model.SelectedNodeId ?? string.Empty,
                model.Nodes
                    .Select(static node => new WireframeNodeEvidence(
                        node.Id,
                        node.TypeName,
                        string.IsNullOrWhiteSpace(node.Name) ? node.AutomationName : node.Name,
                        node.AbsoluteBounds.X,
                        node.AbsoluteBounds.Y,
                        node.AbsoluteBounds.Width,
                        node.AbsoluteBounds.Height,
                        node.IsVisible,
                        node.IsEnabled,
                        node.IsFocused))
                    .ToArray(),
                [
                    "root-relative DIPs",
                    "current node IDs from the latest tree snapshot",
                    "screenshots used only for visual confirmation",
                    "wireframe hit targets generated from absoluteBounds",
                ]);
        }
    }

    private sealed record WireframeNodeEvidence(
        string Id,
        string TypeName,
        string Label,
        double X,
        double Y,
        double Width,
        double Height,
        bool IsVisible,
        bool IsEnabled,
        bool IsFocused);

    private static class AiUnitRemoteToolReviewPrompt
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        public static string BuildSystemPrompt() =>
            $$"""
            You are the Avalonia Remote Control aiUnit auditor for the running desktop tool.
            Return only JSON. Do not use Markdown fences.
            Use schemaVersion "{{SchemaVersion}}" and scenarioId "{{ScenarioId}}".

            Required JSON shape:
            {
              "schemaVersion": "{{SchemaVersion}}",
              "scenarioId": "{{ScenarioId}}",
              "status": "pass|fail|error",
              "summary": "one concise sentence",
              "mcpInteraction": {
                "status": "pass|fail|unknown",
                "evidence": "specific JSON-RPC evidence from the running avalonia-remote MCP server",
                "toolsUsed": ["MCP tool names exercised"]
              },
              "wireframeVsScreenshot": {
                "status": "pass|fail|unknown",
                "treeFirst": true,
                "visualMatch": "match|deviation|unknown",
                "evidence": "specific comparison evidence",
                "findings": [
                  { "severity": "critical|high|medium|low", "title": "finding title", "detail": "specific problem", "recommendation": "specific fix" }
                ]
              },
              "findings": [
                { "severity": "critical|high|medium|low", "title": "finding title", "detail": "specific problem", "recommendation": "specific fix" }
              ],
              "recommendedTests": ["specific additional deterministic test"]
            }

            The first image is the wireframe baseline built from the current remote control tree.
            The second image is the screenshot captured from live view.
            Do not use screenshots or pixel inspection as the primary way to choose controls; use MCP tree data, node IDs, and absolute bounds first.
            Treat failed JSON-RPC initialize, tools/list, avalonia_remote_get_snapshot, or avalonia_remote_invoke_click evidence as an MCP interaction finding.
            Treat a missing tree-first explanation or missing wireframe-vs-screenshot reasoning as a finding.
            """;

        public static string BuildUserPrompt(RemoteToolEvidence evidence) =>
            """
            Review this Avalonia Remote Control evidence. The scope is the running avalonia-remote tool MCP server and live-view visual behavior, not the external MCP Server workspace marker contract.

            Required review rules:
            - Verify the in-process MCP endpoint initialized, listed tools, returned capabilities, returned a current tree snapshot, and invoked a click on a node found from the tree.
            - Verify the wireframe-vs-screenshot evidence is tree-first: identify controls through avalonia_remote_get_snapshot, then compare screenshot fidelity.
            - Do not use screenshots or pixel inspection as the primary way to choose controls.
            - Use screenshots only for visual confirmation after the control tree and absoluteBounds identify the surface.

            Evidence JSON:
            """
            + Environment.NewLine
            + JsonSerializer.Serialize(evidence, JsonOptions)
            + Environment.NewLine
            + """

            Image attachment order:
            1. wireframe baseline image: arc-live-view-wireframe.png
            2. live-view screenshot image: arc-live-view-screenshot.png

            Return exactly one JSON object matching the required schema.
            """;

        public static IReadOnlyList<FrontierAttachment> BuildAttachments(RemoteToolVisualEvidence visual) =>
        [
            new("image/png", visual.WireframeAttachmentName, RemoteToolPngFactory.CreateWireframe(visual)),
            new("image/png", visual.ScreenshotAttachmentName, RemoteToolPngFactory.CreateScreenshot(visual)),
        ];
    }

    private static class AiUnitRemoteToolReviewValidator
    {
        private static readonly string[] AllowedStatus = ["pass", "fail", "error"];
        private static readonly string[] AllowedSectionStatus = ["pass", "fail", "unknown"];
        private static readonly string[] AllowedSeverity = ["critical", "high", "medium", "low"];
        private static readonly string[] AllowedVisualMatch = ["match", "deviation", "unknown"];

        public static AiUnitRemoteToolReviewResult ParseAndValidate(string json, string expectedScenarioId)
        {
            var extracted = ExtractJsonObject(json);
            using var document = JsonDocument.Parse(extracted);
            var root = document.RootElement;

            AiUnitJsonAssertions.Required(
                root,
                "schemaVersion",
                "scenarioId",
                "status",
                "summary",
                "mcpInteraction",
                "wireframeVsScreenshot",
                "findings",
                "recommendedTests");
            AiUnitJsonAssertions.EnumIn(root, "status", AllowedStatus, StringComparer.OrdinalIgnoreCase);
            AiUnitJsonAssertions.ObjectArrayRequired(root, "findings", "severity", "title", "detail", "recommendation");
            AiUnitJsonAssertions.StringArray(root, "recommendedTests");

            var schemaVersion = root.GetProperty("schemaVersion").GetString();
            if (!string.Equals(schemaVersion, SchemaVersion, StringComparison.Ordinal))
            {
                throw new AiResponseValidationException($"schemaVersion must be {SchemaVersion}.");
            }

            var scenarioId = root.GetProperty("scenarioId").GetString();
            if (!string.Equals(scenarioId, expectedScenarioId, StringComparison.Ordinal))
            {
                throw new AiResponseValidationException($"scenarioId must be {expectedScenarioId}.");
            }

            var mcpInteraction = RequireObject(root, "mcpInteraction");
            AiUnitJsonAssertions.Required(mcpInteraction, "status", "evidence", "toolsUsed");
            AiUnitJsonAssertions.EnumIn(mcpInteraction, "status", AllowedSectionStatus, StringComparer.OrdinalIgnoreCase);
            AiUnitJsonAssertions.StringArray(mcpInteraction, "toolsUsed");

            var visual = RequireObject(root, "wireframeVsScreenshot");
            AiUnitJsonAssertions.Required(visual, "status", "treeFirst", "visualMatch", "evidence", "findings");
            AiUnitJsonAssertions.EnumIn(visual, "status", AllowedSectionStatus, StringComparer.OrdinalIgnoreCase);
            AiUnitJsonAssertions.EnumIn(visual, "visualMatch", AllowedVisualMatch, StringComparer.OrdinalIgnoreCase);
            if (!visual.GetProperty("treeFirst").GetBoolean())
            {
                throw new AiResponseValidationException("wireframeVsScreenshot.treeFirst must be true.");
            }

            AiUnitJsonAssertions.ObjectArrayRequired(visual, "findings", "severity", "title", "detail", "recommendation");

            var findings = ParseFindings(root.GetProperty("findings"))
                .Concat(ParseFindings(visual.GetProperty("findings")))
                .ToArray();

            return new AiUnitRemoteToolReviewResult(
                root.GetProperty("status").GetString() ?? string.Empty,
                mcpInteraction.GetProperty("status").GetString() ?? string.Empty,
                visual.GetProperty("status").GetString() ?? string.Empty,
                findings);
        }

        private static JsonElement RequireObject(JsonElement root, string name)
        {
            var element = root.GetProperty(name);
            if (element.ValueKind != JsonValueKind.Object)
            {
                throw new AiResponseValidationException($"{name} must be an object.");
            }

            return element;
        }

        private static IReadOnlyList<AiUnitRemoteToolFinding> ParseFindings(JsonElement array)
        {
            var findings = new List<AiUnitRemoteToolFinding>();
            foreach (var finding in array.EnumerateArray())
            {
                AiUnitJsonAssertions.EnumIn(finding, "severity", AllowedSeverity, StringComparer.OrdinalIgnoreCase);
                findings.Add(new AiUnitRemoteToolFinding(
                    finding.GetProperty("severity").GetString() ?? string.Empty,
                    finding.GetProperty("title").GetString() ?? string.Empty,
                    finding.GetProperty("recommendation").GetString() ?? string.Empty));
            }

            return findings;
        }

        private static string ExtractJsonObject(string text)
        {
            var start = text.IndexOf('{', StringComparison.Ordinal);
            var end = text.LastIndexOf('}');
            if (start < 0 || end < start)
            {
                throw new AiResponseValidationException("Response did not contain a JSON object.");
            }

            return text[start..(end + 1)];
        }
    }

    private sealed record AiUnitRemoteToolReviewResult(
        string Status,
        string McpInteractionStatus,
        string WireframeVsScreenshotStatus,
        IReadOnlyList<AiUnitRemoteToolFinding> Findings);

    private sealed record AiUnitRemoteToolFinding(
        string Severity,
        string Title,
        string Recommendation);

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
        public const string CapabilitiesJson =
            """
            {
              "protocolVersion": "1.0",
              "supportsTreeSnapshots": true,
              "supportsTreeStreaming": true,
              "supportsClickInvocation": true,
              "supportsPropertyMutation": true,
              "supportsLogStreaming": true,
              "supportsFrameStreaming": true,
              "supportsRemoteInput": true
            }
            """;

        public const string SnapshotJson =
            """
            {
              "sequence": 7,
              "nodes": [
                {
                  "id": "root",
                  "parentId": "",
                  "typeName": "Window",
                  "name": "Settings",
                  "automationName": "Settings",
                  "bounds": { "x": 0, "y": 0, "width": 360, "height": 640 },
                  "absoluteBounds": { "x": 0, "y": 0, "width": 360, "height": 640 },
                  "isVisible": true,
                  "isEnabled": true,
                  "isFocused": false
                },
                {
                  "id": "theme-combo",
                  "parentId": "root",
                  "typeName": "ComboBox",
                  "name": "Theme",
                  "automationName": "Theme",
                  "bounds": { "x": 24, "y": 140, "width": 312, "height": 48 },
                  "absoluteBounds": { "x": 24, "y": 140, "width": 312, "height": 48 },
                  "isVisible": true,
                  "isEnabled": true,
                  "isFocused": false
                },
                {
                  "id": "save-button",
                  "parentId": "root",
                  "typeName": "Button",
                  "name": "Save",
                  "automationName": "Save",
                  "bounds": { "x": 190, "y": 420, "width": 120, "height": 48 },
                  "absoluteBounds": { "x": 190, "y": 420, "width": 120, "height": 48 },
                  "isVisible": true,
                  "isEnabled": true,
                  "isFocused": true
                }
              ]
            }
            """;

        public string? ClickedNodeId { get; private set; }

        public Task<string> GetCapabilitiesJsonAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CapabilitiesJson);

        public Task<string> GetSnapshotJsonAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SnapshotJson);

        public Task<string> InvokeClickJsonAsync(string nodeId, CancellationToken cancellationToken = default)
        {
            ClickedNodeId = nodeId;
            return Task.FromResult($$"""{"success":true,"message":"clicked {{nodeId}}"}""");
        }

        public Task<string> FocusJsonAsync(string nodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult($$"""{"success":true,"message":"focused {{nodeId}}"}""");

        public Task<string> SetPropertyJsonAsync(
            string nodeId,
            string propertyName,
            string value,
            CancellationToken cancellationToken = default) =>
            Task.FromResult($$"""{"success":true,"message":"set {{propertyName}} on {{nodeId}}"}""");

        public void Dispose()
        {
        }
    }

    private static TreeSnapshot CreateSampleSnapshot()
    {
        return new TreeSnapshot
        {
            Sequence = 7,
            Nodes =
            {
                Node("root", null, "Window", "Settings", 0, 0, 360, 640),
                Node("theme-combo", "root", "ComboBox", "Theme", 24, 140, 312, 48),
                Node("save-button", "root", "Button", "Save", 190, 420, 120, 48, isFocused: true),
            },
        };
    }

    private static TreeNode Node(
        string id,
        string? parentId,
        string typeName,
        string name,
        double x,
        double y,
        double width,
        double height,
        bool isFocused = false)
    {
        return new TreeNode
        {
            Id = id,
            ParentId = parentId ?? string.Empty,
            TypeName = typeName,
            Name = name,
            AutomationName = name,
            IsVisible = true,
            IsEnabled = true,
            IsFocused = isFocused,
            Bounds = new ProtocolRect
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            },
            AbsoluteBounds = new ProtocolRect
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
            },
        };
    }

    private static class RemoteToolPngFactory
    {
        public static byte[] CreateWireframe(RemoteToolVisualEvidence visual)
        {
            var canvas = new RgbCanvas(visual.Width, visual.Height, new Rgb(18, 18, 18));
            foreach (var node in visual.WireframeNodes)
            {
                var color = node.Id == visual.PrimaryInteractionNodeId
                    ? new Rgb(0, 122, 204)
                    : new Rgb(160, 160, 160);
                canvas.StrokeRect(
                    (int)Math.Round(node.X),
                    (int)Math.Round(node.Y),
                    (int)Math.Round(node.Width),
                    (int)Math.Round(node.Height),
                    color,
                    thickness: node.Id == "root" ? 2 : 3);
                if (node.Id != "root")
                {
                    canvas.FillRect(
                        (int)Math.Round(node.X) + 8,
                        (int)Math.Round(node.Y) + 8,
                        Math.Max(24, (int)Math.Round(node.Width) / 3),
                        8,
                        color);
                }
            }

            return PngWriter.Write(canvas);
        }

        public static byte[] CreateScreenshot(RemoteToolVisualEvidence visual)
        {
            var canvas = new RgbCanvas(visual.Width, visual.Height, new Rgb(8, 8, 8));
            canvas.FillRect(0, 0, visual.Width, visual.Height, new Rgb(5, 5, 5));
            canvas.FillRect(24, 52, 190, 18, new Rgb(235, 235, 235));
            canvas.StrokeRect(20, 104, 320, 112, new Rgb(152, 152, 152), 2);
            canvas.FillRect(40, 132, 132, 14, new Rgb(220, 220, 220));
            canvas.FillRect(24, 140, 312, 48, new Rgb(236, 236, 236));
            canvas.StrokeRect(20, 240, 320, 112, new Rgb(152, 152, 152), 2);
            canvas.FillRect(40, 268, 180, 14, new Rgb(220, 220, 220));
            canvas.StrokeRect(190, 420, 120, 48, new Rgb(0, 122, 204), 3);
            canvas.FillRect(214, 438, 72, 12, new Rgb(0, 122, 204));
            return PngWriter.Write(canvas);
        }
    }

    private sealed class RgbCanvas
    {
        private readonly Rgb[] pixels;

        public RgbCanvas(int width, int height, Rgb background)
        {
            Width = width;
            Height = height;
            pixels = Enumerable.Repeat(background, width * height).ToArray();
        }

        public int Width { get; }

        public int Height { get; }

        public Rgb this[int x, int y]
        {
            get => pixels[(y * Width) + x];
            set => pixels[(y * Width) + x] = value;
        }

        public void FillRect(int x, int y, int width, int height, Rgb color)
        {
            var left = Math.Clamp(x, 0, Width);
            var top = Math.Clamp(y, 0, Height);
            var right = Math.Clamp(x + width, 0, Width);
            var bottom = Math.Clamp(y + height, 0, Height);
            for (var row = top; row < bottom; row++)
            {
                for (var column = left; column < right; column++)
                {
                    this[column, row] = color;
                }
            }
        }

        public void StrokeRect(int x, int y, int width, int height, Rgb color, int thickness)
        {
            FillRect(x, y, width, thickness, color);
            FillRect(x, y + height - thickness, width, thickness, color);
            FillRect(x, y, thickness, height, color);
            FillRect(x + width - thickness, y, thickness, height, color);
        }

        public byte[] ToFilteredRgbRows()
        {
            var rows = new byte[(Width * Height * 3) + Height];
            var offset = 0;
            for (var y = 0; y < Height; y++)
            {
                rows[offset++] = 0;
                for (var x = 0; x < Width; x++)
                {
                    var pixel = this[x, y];
                    rows[offset++] = pixel.R;
                    rows[offset++] = pixel.G;
                    rows[offset++] = pixel.B;
                }
            }

            return rows;
        }
    }

    private readonly record struct Rgb(byte R, byte G, byte B);

    private static class PngWriter
    {
        private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];
        private static readonly uint[] CrcTable = CreateCrcTable();

        public static byte[] Write(RgbCanvas canvas)
        {
            using var output = new MemoryStream();
            output.Write(Signature);
            WriteChunk(output, "IHDR", CreateHeader(canvas.Width, canvas.Height));
            WriteChunk(output, "IDAT", Deflate(canvas.ToFilteredRgbRows()));
            WriteChunk(output, "IEND", []);
            return output.ToArray();
        }

        private static byte[] CreateHeader(int width, int height)
        {
            var header = new byte[13];
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
            BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
            header[8] = 8;
            header[9] = 2;
            header[10] = 0;
            header[11] = 0;
            header[12] = 0;
            return header;
        }

        private static byte[] Deflate(byte[] data)
        {
            using var output = new MemoryStream();
            using (var deflate = new ZLibStream(output, CompressionLevel.Fastest, leaveOpen: true))
            {
                deflate.Write(data);
            }

            return output.ToArray();
        }

        private static void WriteChunk(Stream output, string type, byte[] data)
        {
            Span<byte> buffer = stackalloc byte[4];
            BinaryPrimitives.WriteInt32BigEndian(buffer, data.Length);
            output.Write(buffer);
            var typeBytes = Encoding.ASCII.GetBytes(type);
            output.Write(typeBytes);
            output.Write(data);

            var crc = UpdateCrc(0xffffffff, typeBytes);
            crc = UpdateCrc(crc, data) ^ 0xffffffff;
            BinaryPrimitives.WriteUInt32BigEndian(buffer, crc);
            output.Write(buffer);
        }

        private static uint UpdateCrc(uint crc, byte[] bytes)
        {
            foreach (var value in bytes)
            {
                crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
            }

            return crc;
        }

        private static uint[] CreateCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < table.Length; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                {
                    c = (c & 1) != 0
                        ? 0xedb88320 ^ (c >> 1)
                        : c >> 1;
                }

                table[n] = c;
            }

            return table;
        }
    }

    private static class PngProbe
    {
        public static (int Width, int Height) ReadDimensions(byte[] bytes)
        {
            if (bytes.Length < 24)
            {
                throw new InvalidDataException("PNG is too small.");
            }

            return (ReadBigEndianInt32(bytes, 16), ReadBigEndianInt32(bytes, 20));
        }

        private static int ReadBigEndianInt32(byte[] bytes, int offset) =>
            (bytes[offset] << 24)
            | (bytes[offset + 1] << 16)
            | (bytes[offset + 2] << 8)
            | bytes[offset + 3];
    }
}
