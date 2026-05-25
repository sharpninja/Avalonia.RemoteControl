using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Security;
using Avalonia.RemoteControl.Server.Snapshots;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using RemoteControlGrpc = Avalonia.RemoteControl.Protocol.V1.RemoteControl;

namespace Avalonia.RemoteControl.Server.Grpc;

/// <summary>
/// Implements the gRPC read-only remote-control surface.
/// </summary>
public sealed class AvaloniaRemoteControlGrpcService : RemoteControlGrpc.RemoteControlBase
{
    private readonly AvaloniaRemoteControlService remoteControlService;
    private readonly IControlTreeSnapshotProvider snapshotProvider;
    private readonly IRemoteControlRootProvider rootProvider;
    private readonly RemoteControlTreeStreamService treeStreamService;
    private readonly RemoteControlLogStreamService logStreamService;
    private readonly RemoteControlActionInvoker actionInvoker;
    private readonly RemoteControlPropertyMutationService propertyMutationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaRemoteControlGrpcService"/> class.
    /// </summary>
    /// <param name="remoteControlService">The remote-control application service.</param>
    /// <param name="snapshotProvider">The read-only tree snapshot provider.</param>
    /// <param name="rootProvider">The host app root provider.</param>
    /// <param name="treeStreamService">The live tree stream service.</param>
    /// <param name="logStreamService">The live log stream service.</param>
    /// <param name="actionInvoker">The remote action invoker.</param>
    /// <param name="propertyMutationService">The remote property mutation service.</param>
    public AvaloniaRemoteControlGrpcService(
        AvaloniaRemoteControlService remoteControlService,
        IControlTreeSnapshotProvider snapshotProvider,
        IRemoteControlRootProvider rootProvider,
        RemoteControlTreeStreamService treeStreamService,
        RemoteControlLogStreamService logStreamService,
        RemoteControlActionInvoker actionInvoker,
        RemoteControlPropertyMutationService propertyMutationService)
    {
        this.remoteControlService = remoteControlService;
        this.snapshotProvider = snapshotProvider;
        this.rootProvider = rootProvider;
        this.treeStreamService = treeStreamService;
        this.logStreamService = logStreamService;
        this.actionInvoker = actionInvoker;
        this.propertyMutationService = propertyMutationService;
    }

    /// <inheritdoc />
    public override Task<GetCapabilitiesResponse> GetCapabilities(
        GetCapabilitiesRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(remoteControlService.GetCapabilities().ToGrpc());
    }

    /// <inheritdoc />
    public override async Task<TreeSnapshot> GetSnapshot(
        GetSnapshotRequest request,
        ServerCallContext context)
    {
        var root = rootProvider.GetRootControl();

        if (root is null)
        {
            throw new RpcException(new Status(
                StatusCode.FailedPrecondition,
                "No Avalonia remote-control root control is registered."));
        }

        var snapshot = await snapshotProvider.CaptureSnapshotAsync(root);
        return snapshot.ToGrpc();
    }

    /// <inheritdoc />
    public override async Task WatchTree(
        WatchTreeRequest request,
        IServerStreamWriter<TreeUpdate> responseStream,
        ServerCallContext context)
    {
        await foreach (var snapshot in treeStreamService.WatchSnapshotsAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(new TreeUpdate
            {
                Sequence = snapshot.Sequence,
                Snapshot = snapshot.ToGrpc(),
                Reason = "periodic",
            });
        }
    }

    /// <inheritdoc />
    public override async Task<CommandResult> InvokeClick(
        InvokeClickRequest request,
        ServerCallContext context)
    {
        var result = await actionInvoker.InvokeClickAsync(
            request.NodeId,
            RemoteControlClientIdentity.From(context));
        return result.ToGrpc();
    }

    /// <inheritdoc />
    public override async Task<CommandResult> InvokeFocus(
        InvokeFocusRequest request,
        ServerCallContext context)
    {
        var result = await actionInvoker.InvokeFocusAsync(
            request.NodeId,
            RemoteControlClientIdentity.From(context));
        return result.ToGrpc();
    }

    /// <inheritdoc />
    public override async Task<CommandResult> SetProperty(
        SetPropertyRequest request,
        ServerCallContext context)
    {
        var result = await propertyMutationService.SetPropertyAsync(
            request.NodeId,
            request.PropertyName,
            request.Value,
            RemoteControlClientIdentity.From(context));

        return result.ToGrpc();
    }

    /// <inheritdoc />
    public override async Task WatchLogs(
        WatchLogsRequest request,
        IServerStreamWriter<LogEntry> responseStream,
        ServerCallContext context)
    {
        var minimumLevel = ParseMinimumLevel(request.MinimumLevel);
        var categoryPrefix = string.IsNullOrWhiteSpace(request.CategoryPrefix)
            ? null
            : request.CategoryPrefix;

        await foreach (var entry in logStreamService.WatchEntriesAsync(
            minimumLevel,
            categoryPrefix,
            context.CancellationToken))
        {
            await responseStream.WriteAsync(entry.ToGrpc());
        }
    }

    private static LogLevel ParseMinimumLevel(string minimumLevel)
    {
        if (string.IsNullOrWhiteSpace(minimumLevel))
        {
            return LogLevel.Trace;
        }

        if (Enum.TryParse<LogLevel>(minimumLevel, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new RpcException(new Status(
            StatusCode.InvalidArgument,
            "minimum_level must be a Microsoft.Extensions.Logging LogLevel name."));
    }
}
