using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Grpc;
using Avalonia.RemoteControl.Server.Input;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Rendering;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlReadOnlyInspectionTests
{
    [Fact]
    public void CapabilitiesExposeIterationOneReadOnlySurface()
    {
        var service = new AvaloniaRemoteControlService();

        var capabilities = service.GetCapabilities();

        Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
        Assert.True(capabilities.SupportsTreeSnapshots);
        Assert.True(capabilities.SupportsTreeStreaming);
        Assert.True(capabilities.SupportsClickInvocation);
        Assert.True(capabilities.SupportsPropertyMutation);
        Assert.True(capabilities.SupportsLogStreaming);
    }

    [Fact]
    public async Task SnapshotProviderCapturesHierarchyAndAutomationMetadata()
    {
        var provider = CreateProvider();
        var root = new StackPanel
        {
            Name = "RootPanel",
            IsVisible = true,
            IsEnabled = true,
        };

        var button = new Button
        {
            Name = "RunButton",
            Content = "Run",
        };

        AutomationProperties.SetName(button, "Run command");
        AutomationProperties.SetAutomationId(button, "run-command");
        button.Classes.Add("primary");
        root.Children.Add(button);

        var snapshot = await provider.CaptureSnapshotAsync(root);

        Assert.Equal(1UL, snapshot.Sequence);
        Assert.Equal(2, snapshot.Nodes.Count);

        var rootNode = snapshot.Nodes[0];
        var buttonNode = snapshot.Nodes[1];

        Assert.Equal("StackPanel", rootNode.TypeName);
        Assert.Equal("RootPanel", rootNode.Name);
        Assert.Null(rootNode.ParentId);

        Assert.Equal(rootNode.Id, buttonNode.ParentId);
        Assert.Equal("Button", buttonNode.TypeName);
        Assert.Equal("RunButton", buttonNode.Name);
        Assert.Equal("Run command", buttonNode.AutomationName);
        Assert.Equal("run-command", buttonNode.AutomationId);
        Assert.Contains("primary", buttonNode.Classes);
    }

    [Fact]
    public async Task SnapshotProviderKeepsStableNodeIdsAcrossSnapshots()
    {
        var provider = CreateProvider();
        var root = new StackPanel();
        var child = new TextBlock { Text = "Stable" };
        root.Children.Add(child);

        var first = await provider.CaptureSnapshotAsync(root);
        var second = await provider.CaptureSnapshotAsync(root);

        Assert.Equal(1UL, first.Sequence);
        Assert.Equal(2UL, second.Sequence);
        Assert.Equal(first.Nodes[0].Id, second.Nodes[0].Id);
        Assert.Equal(first.Nodes[1].Id, second.Nodes[1].Id);
    }

    [Fact]
    public async Task SnapshotProviderRedactsSensitivePublicPropertyValues()
    {
        var provider = CreateProvider();
        var root = new SensitiveTestControl
        {
            AuthToken = "do-not-leak",
        };

        var snapshot = await provider.CaptureSnapshotAsync(root);

        var tokenProperty = Assert.Single(snapshot.Nodes[0].Properties, p => p.Name == nameof(SensitiveTestControl.AuthToken));
        Assert.True(tokenProperty.IsRedacted);
        Assert.Equal("[redacted]", tokenProperty.Value);
    }

    [Fact]
    public async Task GrpcServiceMapsCapabilitiesAndSnapshot()
    {
        var root = new StackPanel { Name = "GrpcRoot" };
        root.Children.Add(new TextBlock { Name = "GrpcChild", Text = "Child" });

        var grpcService = CreateGrpcService(root, CreateProvider(), new AvaloniaRemoteControlOptions());

        var capabilities = await grpcService.GetCapabilities(new GetCapabilitiesRequest(), context: null!);
        var snapshot = await grpcService.GetSnapshot(new GetSnapshotRequest(), context: null!);

        Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
        Assert.True(capabilities.SupportsTreeSnapshots);
        Assert.Equal(2, snapshot.Nodes.Count);
        Assert.Equal("GrpcRoot", snapshot.Nodes[0].Name);
        Assert.Equal(snapshot.Nodes[0].Id, snapshot.Nodes[1].ParentId);
    }

    private sealed class SensitiveTestControl : Control
    {
        public string AuthToken { get; set; } = string.Empty;
    }

    private static AvaloniaControlTreeSnapshotProvider CreateProvider()
    {
        return new AvaloniaControlTreeSnapshotProvider(
            Options.Create(new AvaloniaRemoteControlOptions()),
            new InlineRemoteControlDispatcher());
    }

    private static AvaloniaRemoteControlGrpcService CreateGrpcService(
        Control root,
        AvaloniaControlTreeSnapshotProvider provider,
        AvaloniaRemoteControlOptions options)
    {
        return new AvaloniaRemoteControlGrpcService(
            new AvaloniaRemoteControlService(
                Options.Create(options),
                NullLogger<AvaloniaRemoteControlService>.Instance),
            provider,
            new StaticRemoteControlRootProvider(root),
            new RemoteControlTreeStreamService(
                provider,
                new StaticRemoteControlRootProvider(root),
                Options.Create(options)),
            new RemoteControlLogStreamService(
                new RemoteControlLogBuffer(Options.Create(options))),
            new RemoteControlFrameStreamService(
                new StaticRemoteControlRootProvider(root),
                new StubFrameProvider(),
                Options.Create(options),
                NullLogger<RemoteControlFrameStreamService>.Instance),
            new RemoteControlActionInvoker(
                provider,
                Options.Create(options),
                new InlineRemoteControlDispatcher(),
                NullLogger<RemoteControlActionInvoker>.Instance),
            new RemoteControlPropertyMutationService(
                provider,
                Options.Create(options),
                new InlineRemoteControlDispatcher(),
                NullLogger<RemoteControlPropertyMutationService>.Instance),
            new RemoteControlInputDispatcher(
                new StaticRemoteControlRootProvider(root),
                Options.Create(options),
                new InlineRemoteControlDispatcher(),
                NullLogger<RemoteControlInputDispatcher>.Instance));
    }

    private sealed class StubFrameProvider : IRemoteControlFrameProvider
    {
        public ValueTask<RemoteControlFrame> CaptureFrameAsync(
            Control root,
            ulong sequence,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(new RemoteControlFrame(
                sequence,
                [1],
                1,
                1,
                1,
                1,
                1,
                DateTimeOffset.UtcNow));
        }
    }
}
