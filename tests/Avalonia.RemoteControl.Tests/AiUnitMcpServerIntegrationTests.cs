using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using SharpNinja.AiUnit.Frontier;
using SharpNinja.AiUnit.Validation;
using SharpNinja.AiUnit.Xunit;

namespace Avalonia.RemoteControl.Tests;

public sealed class AiUnitMcpServerIntegrationTests
{
    private const string ScenarioId = "mcp-server-workspace-contract";
    private const string SchemaVersion = "arc.aiunit.mcpServerReview.v1";

    [Fact]
    public void McpServerEvidenceLoaderHandlesWorkspaceMarker()
    {
        var repositoryRoot = McpServerAiUnitScenarioCatalog.RepositoryRoot;
        var evidence = McpServerEvidence.LoadFromWorkspace(repositoryRoot);

        Assert.Equal(repositoryRoot, evidence.RepositoryRoot);
        if (!evidence.MarkerPresent)
        {
            Assert.Equal("missing", evidence.MarkerStatus);
            return;
        }

        Assert.Equal("present", evidence.MarkerStatus);
        Assert.False(string.IsNullOrWhiteSpace(evidence.BaseUrl));
        Assert.Contains("workflow.sessionlog.*", evidence.PluginToolExpectations, StringComparer.Ordinal);
        Assert.Contains("workflow.requirements.*", evidence.PluginToolExpectations, StringComparer.Ordinal);
    }

    [Fact]
    public void AiUnitMcpServerReviewPromptNamesRequiredEvidence()
    {
        var prompt = AiUnitMcpServerPrompt.BuildUserPrompt(McpServerEvidence.Sample());

        Assert.Contains("healthNonceEchoed", prompt, StringComparison.Ordinal);
        Assert.Contains("workflow.requirements.*", prompt, StringComparison.Ordinal);
        Assert.Contains(SchemaVersion, AiUnitMcpServerPrompt.BuildSystemPrompt(), StringComparison.Ordinal);
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
    public void AiUnitMcpServerReviewValidatorAcceptsPassingJson()
    {
        const string json = """
            {
              "schemaVersion": "arc.aiunit.mcpServerReview.v1",
              "scenarioId": "mcp-server-workspace-contract",
              "status": "pass",
              "summary": "The evidence satisfies the MCP Server workspace contract.",
              "verifiedCapabilities": [
                {
                  "id": "marker-signature",
                  "result": "pass",
                  "evidence": "Marker is present and declares signed trust bootstrap."
                }
              ],
              "findings": [],
              "recommendedTests": [
                "Run the live health nonce check when the MCP Server process is available."
              ]
            }
            """;

        var result = AiUnitMcpServerReviewValidator.ParseAndValidate(json, ScenarioId);

        Assert.Equal("pass", result.Status);
        Assert.Empty(result.Findings);
    }

    [Fact]
    public void AiUnitMcpServerReviewValidatorRejectsMissingFindings()
    {
        const string json = """
            {
              "schemaVersion": "arc.aiunit.mcpServerReview.v1",
              "scenarioId": "mcp-server-workspace-contract",
              "status": "pass",
              "summary": "Missing required arrays.",
              "verifiedCapabilities": [],
              "recommendedTests": []
            }
            """;

        var failure = Assert.Throws<AiResponseValidationException>(() =>
            AiUnitMcpServerReviewValidator.ParseAndValidate(json, ScenarioId));

        Assert.Contains("findings", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AiUnitMcpServerLiveReviewRunsWhenExplicitlyEnabled()
    {
        if (!McpServerAiUnitOptions.Enabled)
        {
            return;
        }

        using var timeout = new CancellationTokenSource(McpServerAiUnitOptions.Timeout);
        var evidence = await McpServerEvidence.CollectLiveAsync(
            McpServerAiUnitScenarioCatalog.RepositoryRoot,
            timeout.Token);

        Assert.True(evidence.MarkerPresent, "AGENTS-README-FIRST.yaml is required for the live MCP Server aiUnit review.");

        var fixture = AiStrategyFixture.Default;
        Assert.True(fixture.IsResolved, fixture.SkipReason);

        var request = new FrontierRequest(
            SystemPrompt: AiUnitMcpServerPrompt.BuildSystemPrompt(),
            UserMessage: AiUnitMcpServerPrompt.BuildUserPrompt(evidence),
            Temperature: 0.0,
            RequireJsonOutput: true);

        var response = await fixture.Client!.SendAsync(request, timeout.Token);
        if (response.Error is { } error)
        {
            Assert.Fail($"{error.ErrorCode}: {error.Message}");
        }

        var result = AiUnitMcpServerReviewValidator.ParseAndValidate(
            response.Text ?? string.Empty,
            ScenarioId);
        Assert.NotEqual("error", result.Status);

        var blocking = result.Findings
            .Where(static finding => finding.Severity is "critical" or "high")
            .Select(static finding => $"{finding.Severity}: {finding.Title} - {finding.Recommendation}")
            .ToArray();

        Assert.Empty(blocking);
    }

    private static class McpServerAiUnitOptions
    {
        public static bool Enabled =>
            string.Equals(
                Environment.GetEnvironmentVariable("ARC_AIUNIT_MCP_SERVER_TESTS_ENABLED"),
                "true",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                Environment.GetEnvironmentVariable("ARC_AIUNIT_MCP_SERVER_TESTS_ENABLED"),
                "1",
                StringComparison.Ordinal);

        public static TimeSpan Timeout =>
            int.TryParse(Environment.GetEnvironmentVariable("ARC_AIUNIT_MCP_SERVER_TIMEOUT_SECONDS"), out var seconds)
            && seconds > 0
                ? TimeSpan.FromSeconds(seconds)
                : TimeSpan.FromMinutes(10);
    }

    private static class McpServerAiUnitScenarioCatalog
    {
        public static string RepositoryRoot { get; } = LocateRepositoryRoot();

        private static string LocateRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Avalonia.RemoteControl.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            return Directory.GetCurrentDirectory();
        }
    }

    private sealed record McpServerEvidence(
        string RepositoryRoot,
        bool MarkerPresent,
        string MarkerStatus,
        string BaseUrl,
        string WorkspacePath,
        string HealthEndpoint,
        bool HealthNonceEchoed,
        int? HealthStatusCode,
        string HealthBodySnippet,
        IReadOnlyList<string> PluginToolExpectations,
        string TrustBootstrapSummary,
        string Error)
    {
        public static McpServerEvidence Sample() =>
            new(
                RepositoryRoot: @"F:\GitHub\Avalonia.RemoteControl",
                MarkerPresent: true,
                MarkerStatus: "present",
                BaseUrl: "http://localhost:7147",
                WorkspacePath: @"F:\GitHub\Avalonia.RemoteControl",
                HealthEndpoint: "/health",
                HealthNonceEchoed: true,
                HealthStatusCode: 200,
                HealthBodySnippet: "nonce echoed",
                PluginToolExpectations:
                [
                    "workflow.sessionlog.*",
                    "workflow.todo.*",
                    "workflow.requirements.*",
                ],
                TrustBootstrapSummary: "signed marker with health nonce verification",
                Error: string.Empty);

        public static McpServerEvidence LoadFromWorkspace(string repositoryRoot)
        {
            var markerPath = Path.Combine(repositoryRoot, "AGENTS-README-FIRST.yaml");
            if (!File.Exists(markerPath))
            {
                return new McpServerEvidence(
                    RepositoryRoot: repositoryRoot,
                    MarkerPresent: false,
                    MarkerStatus: "missing",
                    BaseUrl: string.Empty,
                    WorkspacePath: repositoryRoot,
                    HealthEndpoint: string.Empty,
                    HealthNonceEchoed: false,
                    HealthStatusCode: null,
                    HealthBodySnippet: string.Empty,
                    PluginToolExpectations: [],
                    TrustBootstrapSummary: string.Empty,
                    Error: "AGENTS-README-FIRST.yaml was not found.");
            }

            var marker = File.ReadAllText(markerPath);
            return new McpServerEvidence(
                RepositoryRoot: repositoryRoot,
                MarkerPresent: true,
                MarkerStatus: "present",
                BaseUrl: ExtractScalar(marker, "baseUrl"),
                WorkspacePath: ExtractScalar(marker, "workspacePath"),
                HealthEndpoint: ExtractScalar(marker, "health"),
                HealthNonceEchoed: false,
                HealthStatusCode: null,
                HealthBodySnippet: string.Empty,
                PluginToolExpectations: ExtractToolExpectations(marker),
                TrustBootstrapSummary: marker.Contains("trust_bootstrap:", StringComparison.Ordinal)
                    ? "trust bootstrap present"
                    : "trust bootstrap missing",
                Error: string.Empty);
        }

        public static async Task<McpServerEvidence> CollectLiveAsync(
            string repositoryRoot,
            CancellationToken cancellationToken)
        {
            var evidence = LoadFromWorkspace(repositoryRoot);
            if (!evidence.MarkerPresent || string.IsNullOrWhiteSpace(evidence.BaseUrl))
            {
                return evidence;
            }

            try
            {
                var nonce = "aiunit-" + Guid.NewGuid().ToString("N");
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var healthPath = string.IsNullOrWhiteSpace(evidence.HealthEndpoint)
                    ? "/health"
                    : evidence.HealthEndpoint;
                var uri = new Uri(new Uri(evidence.BaseUrl.TrimEnd('/') + "/"), healthPath.TrimStart('/'));
                var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&";
                var nonceUri = new Uri(uri + separator + "nonce=" + Uri.EscapeDataString(nonce));
                using var response = await http.GetAsync(nonceUri, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                return evidence with
                {
                    HealthNonceEchoed = body.Contains(nonce, StringComparison.Ordinal),
                    HealthStatusCode = (int)response.StatusCode,
                    HealthBodySnippet = Truncate(body, 600),
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return evidence with
                {
                    Error = ex.Message,
                };
            }
        }

        private static string ExtractScalar(string marker, string key)
        {
            var match = Regex.Match(
                marker,
                $@"(?m)^\s*{Regex.Escape(key)}:\s*(?<value>.+?)\s*$",
                RegexOptions.CultureInvariant);
            return match.Success
                ? match.Groups["value"].Value.Trim().Trim('"', '\'')
                : string.Empty;
        }

        private static IReadOnlyList<string> ExtractToolExpectations(string marker)
        {
            var expectations = new[]
            {
                "workflow.sessionlog.*",
                "workflow.todo.*",
                "workflow.requirements.*",
            };
            return expectations
                .Where(expectation => marker.Contains(expectation, StringComparison.Ordinal))
                .ToArray();
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value;
            }

            return value[..maxLength];
        }
    }

    private static class AiUnitMcpServerPrompt
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
        };

        public static string BuildSystemPrompt() =>
            $$"""
            You are validating an MCP Server workspace contract from sanitized test evidence.
            Return only JSON. Do not use Markdown fences.
            Use schemaVersion "{{SchemaVersion}}" and scenarioId "{{ScenarioId}}".
            Required JSON shape:
            {
              "schemaVersion": "{{SchemaVersion}}",
              "scenarioId": "{{ScenarioId}}",
              "status": "pass|fail|error",
              "summary": "one concise sentence",
              "verifiedCapabilities": [
                { "id": "marker|health|plugin|requirements", "result": "pass|fail|unknown", "evidence": "specific evidence" }
              ],
              "findings": [
                { "severity": "critical|high|medium|low", "title": "finding title", "detail": "specific problem", "recommendation": "specific fix" }
              ],
              "recommendedTests": ["specific additional deterministic test"]
            }
            Treat missing marker signature, failed health nonce echo, missing workflow.requirements.* tool expectation, or unactionable evidence as findings.
            """;

        public static string BuildUserPrompt(McpServerEvidence evidence) =>
            "Review this MCP Server evidence for workspace trust, health nonce, Codex plugin, and requirements-tooling coverage." +
            Environment.NewLine +
            JsonSerializer.Serialize(evidence, JsonOptions);
    }

    private static class AiUnitMcpServerReviewValidator
    {
        private static readonly string[] AllowedStatus = ["pass", "fail", "error"];
        private static readonly string[] AllowedResult = ["pass", "fail", "unknown"];
        private static readonly string[] AllowedSeverity = ["critical", "high", "medium", "low"];

        public static AiUnitMcpServerReviewResult ParseAndValidate(string json, string expectedScenarioId)
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
                "verifiedCapabilities",
                "findings",
                "recommendedTests");
            AiUnitJsonAssertions.EnumIn(root, "status", AllowedStatus, StringComparer.OrdinalIgnoreCase);
            AiUnitJsonAssertions.ObjectArrayRequired(root, "verifiedCapabilities", "id", "result", "evidence");
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

            foreach (var capability in root.GetProperty("verifiedCapabilities").EnumerateArray())
            {
                AiUnitJsonAssertions.EnumIn(capability, "result", AllowedResult, StringComparer.OrdinalIgnoreCase);
            }

            var findings = new List<AiUnitMcpServerFinding>();
            foreach (var finding in root.GetProperty("findings").EnumerateArray())
            {
                AiUnitJsonAssertions.EnumIn(finding, "severity", AllowedSeverity, StringComparer.OrdinalIgnoreCase);
                findings.Add(new AiUnitMcpServerFinding(
                    finding.GetProperty("severity").GetString() ?? string.Empty,
                    finding.GetProperty("title").GetString() ?? string.Empty,
                    finding.GetProperty("recommendation").GetString() ?? string.Empty));
            }

            return new AiUnitMcpServerReviewResult(
                root.GetProperty("status").GetString() ?? string.Empty,
                findings);
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

    private sealed record AiUnitMcpServerReviewResult(
        string Status,
        IReadOnlyList<AiUnitMcpServerFinding> Findings);

    private sealed record AiUnitMcpServerFinding(
        string Severity,
        string Title,
        string Recommendation);
}
