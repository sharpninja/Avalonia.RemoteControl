using Avalonia.Controls;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Grpc;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Avalonia.RemoteControl.Protocol.V1;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlCommandTests
{
    [Fact]
    public async Task PropertyMutationDeniesByDefault()
    {
        var root = new TextBlock { Text = "Before" };
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var mutation = CreateMutationService(provider, new AvaloniaRemoteControlOptions());

        var result = await mutation.SetPropertyAsync(snapshot.Nodes[0].Id, nameof(TextBlock.Text), "After");

        Assert.False(result.Succeeded);
        Assert.Equal("Before", root.Text);
    }

    [Fact]
    public async Task PropertyMutationSetsAllowedStringProperty()
    {
        var root = new TextBlock { Text = "Before" };
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var options = new AvaloniaRemoteControlOptions();
        options.AllowedMutableProperties.Add(nameof(TextBlock.Text));
        var mutation = CreateMutationService(provider, options);

        var result = await mutation.SetPropertyAsync(snapshot.Nodes[0].Id, nameof(TextBlock.Text), "After");

        Assert.True(result.Succeeded);
        Assert.Equal("After", root.Text);
    }

    [Fact]
    public async Task PropertyMutationBlocksSensitivePropertyEvenWhenAllowed()
    {
        var root = new SensitiveActionTestControl { AuthToken = "Before" };
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var options = new AvaloniaRemoteControlOptions();
        options.AllowedMutableProperties.Add(nameof(SensitiveActionTestControl.AuthToken));
        var mutation = CreateMutationService(provider, options);

        var result = await mutation.SetPropertyAsync(snapshot.Nodes[0].Id, nameof(SensitiveActionTestControl.AuthToken), "After");

        Assert.False(result.Succeeded);
        Assert.Equal("Before", root.AuthToken);
    }

    [Fact]
    public async Task ClickInvocationRequiresExplicitActionEnablement()
    {
        var clicked = false;
        var root = new Button();
        root.Click += (_, _) => clicked = true;
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var invoker = CreateActionInvoker(provider, new AvaloniaRemoteControlOptions());

        var result = await invoker.InvokeClickAsync(snapshot.Nodes[0].Id);

        Assert.False(result.Succeeded);
        Assert.False(clicked);
    }

    [Fact]
    public async Task ClickInvocationRaisesButtonClickWhenEnabled()
    {
        var clicked = false;
        var root = new Button();
        root.Click += (_, _) => clicked = true;
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var invoker = CreateActionInvoker(provider, new AvaloniaRemoteControlOptions { AllowRemoteActions = true });

        var result = await invoker.InvokeClickAsync(snapshot.Nodes[0].Id);

        Assert.True(result.Succeeded);
        Assert.True(clicked);
    }

    [Fact]
    public async Task GrpcSetPropertyUsesMutationPolicy()
    {
        var root = new TextBlock { Text = "Before" };
        var provider = CreateSnapshotProvider();
        var options = new AvaloniaRemoteControlOptions();
        options.AllowedMutableProperties.Add(nameof(TextBlock.Text));
        var grpc = CreateGrpcService(root, provider, options);
        var snapshot = await grpc.GetSnapshot(new GetSnapshotRequest(), context: null!);

        var result = await grpc.SetProperty(
            new SetPropertyRequest
            {
                NodeId = snapshot.Nodes[0].Id,
                PropertyName = nameof(TextBlock.Text),
                Value = "After",
            },
            context: null!);

        Assert.True(result.Succeeded);
        Assert.Equal("After", root.Text);
    }

    [Fact]
    public async Task GrpcInvokeClickUsesActionPolicy()
    {
        var clicked = false;
        var root = new Button();
        root.Click += (_, _) => clicked = true;
        var provider = CreateSnapshotProvider();
        var options = new AvaloniaRemoteControlOptions { AllowRemoteActions = true };
        var grpc = CreateGrpcService(root, provider, options);
        var snapshot = await grpc.GetSnapshot(new GetSnapshotRequest(), context: null!);

        var result = await grpc.InvokeClick(
            new InvokeClickRequest { NodeId = snapshot.Nodes[0].Id },
            context: null!);

        Assert.True(result.Succeeded);
        Assert.True(clicked);
    }

    private static AvaloniaControlTreeSnapshotProvider CreateSnapshotProvider()
    {
        return new AvaloniaControlTreeSnapshotProvider(
            Options.Create(new AvaloniaRemoteControlOptions()),
            new InlineRemoteControlDispatcher());
    }

    private static RemoteControlPropertyMutationService CreateMutationService(
        IRemoteControlNodeResolver resolver,
        AvaloniaRemoteControlOptions options)
    {
        return new RemoteControlPropertyMutationService(
            resolver,
            Options.Create(options),
            new InlineRemoteControlDispatcher(),
            NullLogger<RemoteControlPropertyMutationService>.Instance);
    }

    private static RemoteControlActionInvoker CreateActionInvoker(
        IRemoteControlNodeResolver resolver,
        AvaloniaRemoteControlOptions options)
    {
        return new RemoteControlActionInvoker(
            resolver,
            Options.Create(options),
            new InlineRemoteControlDispatcher(),
            NullLogger<RemoteControlActionInvoker>.Instance);
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
            CreateActionInvoker(provider, options),
            CreateMutationService(provider, options));
    }

    private sealed class SensitiveActionTestControl : Control
    {
        public string AuthToken { get; set; } = string.Empty;
    }
}
