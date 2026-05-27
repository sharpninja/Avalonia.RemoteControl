using Avalonia.RemoteControl.Client.Projects;
using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlProjectSystemTests
{
    [Fact]
    public async Task ProjectStoreSavesAppSessionsLogsAndReplayArtifacts()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Avalonia.RemoteControl.Tests", Guid.NewGuid().ToString("N"));
        var store = new FileRemoteControlProjectStore(directory);
        var profile = new RemoteControlConnectionProfile
        {
            AppId = "app.funwashad",
            DisplayName = "FunWasHad",
            Endpoint = "http://127.0.0.1:47100/",
            Token = "dev-token",
            TransportProtocol = RemoteControlProtocol.AndroidBridgeTransportProtocol,
            AndroidPackageName = "app.funwashad",
            AndroidSerial = "ZD222QH58Q",
            AdbHostPort = 47100,
            UpdatedUtc = DateTimeOffset.Parse("2026-05-26T12:00:00Z"),
        };
        var document = RemoteControlProjectDocument.Create("project-funwashad", "FunWasHad");
        document.UpsertAppProfile(profile);

        var session = RemoteControlProjectSessionRecord.Start(
            "session-1",
            profile.AppId,
            profile,
            DateTimeOffset.Parse("2026-05-26T12:01:00Z"));
        session.Logs.Add(RemoteControlProjectLogRecord.FromDisplayRow(
            "client 2026-05-26T12:01:00.0000000Z: connected",
            DateTimeOffset.Parse("2026-05-26T12:01:00Z")));

        var afterSnapshot = RemoteControlProjectTreeSnapshot.FromProtocol(CreateSnapshot("Ready"));
        session.Artifacts.Add(RemoteControlReplayArtifact.FromTreeSnapshot(
            "artifact-after-click",
            afterSnapshot,
            DateTimeOffset.Parse("2026-05-26T12:01:02Z")));
        session.Interactions.Add(new RemoteControlInteractionRecord
        {
            StepId = "step-1",
            Order = 1,
            Kind = RemoteControlInteractionKind.Click,
            TimestampUtc = DateTimeOffset.Parse("2026-05-26T12:01:01Z"),
            NodeId = "button-1",
            AfterSnapshotArtifactId = "artifact-after-click",
            ResultSucceeded = true,
            ResultMessage = "Clicked.",
        });
        document.Sessions.Add(session);

        await store.SaveAsync(document);
        var loaded = await store.LoadAsync("project-funwashad");

        Assert.NotNull(loaded);
        Assert.Equal(1, loaded.SchemaVersion);
        Assert.Equal("FunWasHad", loaded.Name);
        Assert.Single(loaded.AppProfiles);
        Assert.Equal(RemoteControlProtocol.AndroidBridgeTransportProtocol, loaded.AppProfiles[0].TransportProtocol);
        Assert.Equal("app.funwashad", loaded.AppProfiles[0].AndroidPackageName);
        Assert.Single(loaded.Sessions);
        Assert.Single(loaded.Sessions[0].Logs);
        Assert.Single(loaded.Sessions[0].Interactions);
        Assert.Single(loaded.Sessions[0].Artifacts);
        Assert.Equal("Ready", loaded.Sessions[0].Artifacts[0].TreeSnapshot?.Nodes[1].Name);
    }

    [Fact]
    public async Task ProjectStoreSavesClientLayoutState()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Avalonia.RemoteControl.Tests", Guid.NewGuid().ToString("N"));
        var store = new FileRemoteControlProjectStore(directory);
        var document = RemoteControlProjectDocument.Create("project-layout", "Layout");
        document.ClientLayout = new RemoteControlClientLayoutState
        {
            WindowWidth = 1440,
            WindowHeight = 900,
            WindowX = 120,
            WindowY = 80,
            WindowState = "Normal",
            TreePaneWidth = 360,
            RightPaneWidth = 420,
            LogPaneHeight = 260,
            RightToolTabIndex = 1,
            WorkspaceTabIndex = 1,
            LogsPoppedOut = true,
            LiveViewDocked = true,
            ControlTreeAutoHidden = true,
            PropertiesAutoHidden = false,
            RemoteToolsAutoHidden = true,
            LogsAutoHidden = false,
        };

        await store.SaveAsync(document);
        var loaded = await store.LoadAsync("project-layout");

        Assert.NotNull(loaded);
        Assert.Equal(1440, loaded.ClientLayout.WindowWidth);
        Assert.Equal(900, loaded.ClientLayout.WindowHeight);
        Assert.Equal(120, loaded.ClientLayout.WindowX);
        Assert.Equal(80, loaded.ClientLayout.WindowY);
        Assert.Equal("Normal", loaded.ClientLayout.WindowState);
        Assert.Equal(360, loaded.ClientLayout.TreePaneWidth);
        Assert.Equal(420, loaded.ClientLayout.RightPaneWidth);
        Assert.Equal(260, loaded.ClientLayout.LogPaneHeight);
        Assert.Equal(1, loaded.ClientLayout.RightToolTabIndex);
        Assert.Equal(1, loaded.ClientLayout.WorkspaceTabIndex);
        Assert.True(loaded.ClientLayout.LogsPoppedOut);
        Assert.True(loaded.ClientLayout.LiveViewDocked);
        Assert.True(loaded.ClientLayout.ControlTreeAutoHidden);
        Assert.False(loaded.ClientLayout.PropertiesAutoHidden);
        Assert.True(loaded.ClientLayout.RemoteToolsAutoHidden);
        Assert.False(loaded.ClientLayout.LogsAutoHidden);
    }

    [Fact]
    public void ReplayDiffReportsAddedRemovedChangedAndUnchangedNodes()
    {
        var original = RemoteControlProjectTreeSnapshot.FromProtocol(CreateSnapshot("Before"));
        var replayed = new RemoteControlProjectTreeSnapshot
        {
            Sequence = 2,
            Nodes =
            {
                original.Nodes[0],
                original.Nodes[1] with { Name = "After" },
                new RemoteControlProjectTreeNode
                {
                    Id = "new-node",
                    ParentId = "root",
                    TypeName = "TextBlock",
                    Name = "Added",
                    IsVisible = true,
                    IsEnabled = true,
                },
            },
        };

        var diff = RemoteControlReplayDiffService.CompareTreeSnapshots(original, replayed);

        Assert.Contains(diff.NodeDiffs, node => node.NodeId == "root" && node.Kind == RemoteControlReplayDiffKind.Unchanged);
        var changed = Assert.Single(diff.NodeDiffs, node => node.NodeId == "button-1");
        Assert.Equal(RemoteControlReplayDiffKind.Changed, changed.Kind);
        Assert.Contains(changed.Changes, change => change.PropertyName == nameof(RemoteControlProjectTreeNode.Name));
        Assert.Contains(diff.NodeDiffs, node => node.NodeId == "new-node" && node.Kind == RemoteControlReplayDiffKind.Added);
    }

    [Fact]
    public async Task ReplayServiceInvokesTargetAndDiffsEachStep()
    {
        var originalAfter = RemoteControlProjectTreeSnapshot.FromProtocol(CreateSnapshot("Before"));
        var replayAfter = RemoteControlProjectTreeSnapshot.FromProtocol(CreateSnapshot("After"));
        var session = RemoteControlProjectSessionRecord.Start(
            "session-1",
            "app.funwashad",
            new RemoteControlConnectionProfile { AppId = "app.funwashad", Endpoint = "http://127.0.0.1:47100/" },
            DateTimeOffset.Parse("2026-05-26T12:01:00Z"));
        session.Artifacts.Add(RemoteControlReplayArtifact.FromTreeSnapshot(
            "artifact-after",
            originalAfter,
            DateTimeOffset.Parse("2026-05-26T12:01:02Z")));
        session.Interactions.Add(new RemoteControlInteractionRecord
        {
            StepId = "step-1",
            Order = 1,
            Kind = RemoteControlInteractionKind.Click,
            NodeId = "button-1",
            AfterSnapshotArtifactId = "artifact-after",
        });
        var target = new RecordingReplayTarget(replayAfter);
        var service = new RemoteControlSessionReplayService();

        var result = await service.ReplayAsync(session, target);

        Assert.Equal(["click:button-1"], target.Invocations);
        var step = Assert.Single(result.Steps);
        Assert.True(step.CommandSucceeded);
        Assert.Equal(RemoteControlInteractionKind.Click, step.Kind);
        Assert.Contains(step.Diff.NodeDiffs, node => node.NodeId == "button-1" && node.Kind == RemoteControlReplayDiffKind.Changed);
    }

    [Fact]
    public void InteractionSummaryDoesNotExposeSensitivePayloadValues()
    {
        var interaction = new RemoteControlInteractionRecord
        {
            StepId = "step-text",
            Order = 1,
            Kind = RemoteControlInteractionKind.InputBatch,
            InputEvents =
            {
                new RemoteControlInputEventRecord
                {
                    Kind = RemoteInputKind.Text.ToString(),
                    Text = "secret typed value",
                    IsSensitive = true,
                },
            },
            SensitiveFields = { "input[0].text" },
        };

        var summary = interaction.ToSanitizedSummary();

        Assert.Contains("InputBatch", summary, StringComparison.Ordinal);
        Assert.Contains("input[0].text", interaction.SensitiveFields);
        Assert.DoesNotContain("secret typed value", summary, StringComparison.Ordinal);
    }

    private static TreeSnapshot CreateSnapshot(string buttonName)
    {
        var snapshot = new TreeSnapshot { Sequence = 1 };
        snapshot.Nodes.Add(new TreeNode
        {
            Id = "root",
            TypeName = "Window",
            Name = "Root",
            IsVisible = true,
            IsEnabled = true,
        });
        snapshot.Nodes.Add(new TreeNode
        {
            Id = "button-1",
            ParentId = "root",
            TypeName = "Button",
            Name = buttonName,
            AutomationId = "start",
            IsVisible = true,
            IsEnabled = true,
            AbsoluteBounds = new Avalonia.RemoteControl.Protocol.V1.Rect
            {
                X = 10,
                Y = 20,
                Width = 100,
                Height = 30,
            },
        });
        snapshot.Nodes[1].Properties.Add(new PropertyValue
        {
            Name = "Content",
            Value = buttonName,
            ValueType = "System.String",
            CanWrite = true,
        });

        return snapshot;
    }

    private sealed class RecordingReplayTarget : IRemoteControlReplayTarget
    {
        private readonly RemoteControlProjectTreeSnapshot snapshot;

        public RecordingReplayTarget(RemoteControlProjectTreeSnapshot snapshot)
        {
            this.snapshot = snapshot;
        }

        public List<string> Invocations { get; } = [];

        public Task<RemoteControlReplayCommandResult> InvokeClickAsync(
            string nodeId,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add($"click:{nodeId}");
            return Task.FromResult(RemoteControlReplayCommandResult.Success("Clicked."));
        }

        public Task<RemoteControlReplayCommandResult> InvokeFocusAsync(
            string nodeId,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add($"focus:{nodeId}");
            return Task.FromResult(RemoteControlReplayCommandResult.Success("Focused."));
        }

        public Task<RemoteControlReplayCommandResult> SetPropertyAsync(
            string nodeId,
            string propertyName,
            string value,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add($"set:{nodeId}:{propertyName}={value}");
            return Task.FromResult(RemoteControlReplayCommandResult.Success("Set."));
        }

        public Task<RemoteControlReplayCommandResult> SendInputAsync(
            IReadOnlyList<RemoteControlInputEventRecord> events,
            CancellationToken cancellationToken = default)
        {
            Invocations.Add($"input:{events.Count}");
            return Task.FromResult(RemoteControlReplayCommandResult.Success("Input."));
        }

        public Task<RemoteControlProjectTreeSnapshot> CaptureTreeSnapshotAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(snapshot);
        }
    }
}
