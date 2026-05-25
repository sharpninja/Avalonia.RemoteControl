using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Grpc;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Security;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Avalonia.RemoteControl.Protocol.V1;
using Microsoft.Extensions.Logging;
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
    public async Task PropertyMutationSetsAllowedAvaloniaValueTypes()
    {
        var root = new ConversionActionTestControl();
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var options = new AvaloniaRemoteControlOptions();
        options.AllowedMutableProperties.Add(nameof(ConversionActionTestControl.TestThickness));
        options.AllowedMutableProperties.Add(nameof(ConversionActionTestControl.TestCornerRadius));
        options.AllowedMutableProperties.Add(nameof(ConversionActionTestControl.TestPoint));
        options.AllowedMutableProperties.Add(nameof(ConversionActionTestControl.TestSize));
        options.AllowedMutableProperties.Add(nameof(ConversionActionTestControl.TestRect));
        options.AllowedMutableProperties.Add(nameof(ConversionActionTestControl.TestBrush));
        var mutation = CreateMutationService(provider, options);
        var nodeId = snapshot.Nodes[0].Id;

        Assert.True((await mutation.SetPropertyAsync(nodeId, nameof(ConversionActionTestControl.TestThickness), "1,2,3,4")).Succeeded);
        Assert.True((await mutation.SetPropertyAsync(nodeId, nameof(ConversionActionTestControl.TestCornerRadius), "5")).Succeeded);
        Assert.True((await mutation.SetPropertyAsync(nodeId, nameof(ConversionActionTestControl.TestPoint), "6,7")).Succeeded);
        Assert.True((await mutation.SetPropertyAsync(nodeId, nameof(ConversionActionTestControl.TestSize), "8,9")).Succeeded);
        Assert.True((await mutation.SetPropertyAsync(nodeId, nameof(ConversionActionTestControl.TestRect), "10,11,12,13")).Succeeded);
        Assert.True((await mutation.SetPropertyAsync(nodeId, nameof(ConversionActionTestControl.TestBrush), "#ff336699")).Succeeded);

        Assert.Equal(new Thickness(1, 2, 3, 4), root.TestThickness);
        Assert.Equal(new CornerRadius(5), root.TestCornerRadius);
        Assert.Equal(new Point(6, 7), root.TestPoint);
        Assert.Equal(new Size(8, 9), root.TestSize);
        Assert.Equal(new Rect(10, 11, 12, 13), root.TestRect);
        var brush = Assert.IsType<SolidColorBrush>(root.TestBrush);
        Assert.Equal(Color.Parse("#ff336699"), brush.Color);
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
    public async Task ClickInvocationAuditLogIncludesClientIdentity()
    {
        var root = new Button();
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var logger = new CapturingLogger<RemoteControlActionInvoker>();
        var invoker = new RemoteControlActionInvoker(
            provider,
            Options.Create(new AvaloniaRemoteControlOptions { AllowRemoteActions = true }),
            new InlineRemoteControlDispatcher(),
            logger);

        await invoker.InvokeClickAsync(snapshot.Nodes[0].Id, "desktop-client");

        Assert.Contains(logger.Messages, message => message.Contains("desktop-client", StringComparison.Ordinal));
    }

    [Fact]
    public async Task FocusInvocationRequiresExplicitActionEnablement()
    {
        var root = new Button();
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var invoker = CreateActionInvoker(provider, new AvaloniaRemoteControlOptions());

        var result = await invoker.InvokeFocusAsync(snapshot.Nodes[0].Id);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task FocusInvocationRequestsFocusWhenEnabled()
    {
        var root = new Button();
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var invoker = CreateActionInvoker(provider, new AvaloniaRemoteControlOptions { AllowRemoteActions = true });

        var result = await invoker.InvokeFocusAsync(snapshot.Nodes[0].Id);

        Assert.True(result.Succeeded);
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

    [Fact]
    public async Task GrpcInvokeFocusUsesActionPolicy()
    {
        var root = new Button();
        var provider = CreateSnapshotProvider();
        var options = new AvaloniaRemoteControlOptions { AllowRemoteActions = true };
        var grpc = CreateGrpcService(root, provider, options);
        var snapshot = await grpc.GetSnapshot(new GetSnapshotRequest(), context: null!);

        var result = await grpc.InvokeFocus(
            new InvokeFocusRequest { NodeId = snapshot.Nodes[0].Id },
            context: null!);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task PropertyMutationAuditLogIncludesClientIdentity()
    {
        var root = new TextBlock { Text = "Before" };
        var provider = CreateSnapshotProvider();
        var snapshot = await provider.CaptureSnapshotAsync(root);
        var options = new AvaloniaRemoteControlOptions();
        options.AllowedMutableProperties.Add(nameof(TextBlock.Text));
        var logger = new CapturingLogger<RemoteControlPropertyMutationService>();
        var mutation = new RemoteControlPropertyMutationService(
            provider,
            Options.Create(options),
            new InlineRemoteControlDispatcher(),
            logger);

        await mutation.SetPropertyAsync(snapshot.Nodes[0].Id, nameof(TextBlock.Text), "After", "desktop-client");

        Assert.Contains(logger.Messages, message => message.Contains("desktop-client", StringComparison.Ordinal));
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

    private sealed class ConversionActionTestControl : Control
    {
        public Thickness TestThickness { get; set; }

        public CornerRadius TestCornerRadius { get; set; }

        public Point TestPoint { get; set; }

        public Size TestSize { get; set; }

        public Rect TestRect { get; set; }

        public IBrush? TestBrush { get; set; }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
