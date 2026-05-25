using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Input;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Protocol;
using Avalonia.RemoteControl.Server.Rendering;
using Avalonia.RemoteControl.Server.Runtime;
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
    private readonly IRemoteControlRuntime runtime;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaRemoteControlGrpcService"/> class.
    /// </summary>
    /// <param name="runtime">Transport-independent runtime service.</param>
    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public AvaloniaRemoteControlGrpcService(IRemoteControlRuntime runtime)
    {
        this.runtime = runtime;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaRemoteControlGrpcService"/> class.
    /// </summary>
    /// <param name="remoteControlService">The remote-control application service.</param>
    /// <param name="snapshotProvider">The read-only tree snapshot provider.</param>
    /// <param name="rootProvider">The host app root provider.</param>
    /// <param name="treeStreamService">The live tree stream service.</param>
    /// <param name="logStreamService">The live log stream service.</param>
    /// <param name="frameStreamService">The live frame stream service.</param>
    /// <param name="actionInvoker">The remote action invoker.</param>
    /// <param name="propertyMutationService">The remote property mutation service.</param>
    /// <param name="inputDispatcher">The live remote input dispatcher.</param>
    public AvaloniaRemoteControlGrpcService(
        AvaloniaRemoteControlService remoteControlService,
        IControlTreeSnapshotProvider snapshotProvider,
        IRemoteControlRootProvider rootProvider,
        RemoteControlTreeStreamService treeStreamService,
        RemoteControlLogStreamService logStreamService,
        RemoteControlFrameStreamService frameStreamService,
        RemoteControlActionInvoker actionInvoker,
        RemoteControlPropertyMutationService propertyMutationService,
        RemoteControlInputDispatcher inputDispatcher)
        : this(new RemoteControlRuntime(
            remoteControlService,
            snapshotProvider,
            rootProvider,
            treeStreamService,
            logStreamService,
            frameStreamService,
            actionInvoker,
            propertyMutationService,
            inputDispatcher))
    {
    }

    /// <inheritdoc />
    public override Task<GetCapabilitiesResponse> GetCapabilities(
        GetCapabilitiesRequest request,
        ServerCallContext context)
    {
        return Task.FromResult(runtime.GetCapabilities().ToProtocol());
    }

    /// <inheritdoc />
    public override async Task<TreeSnapshot> GetSnapshot(
        GetSnapshotRequest request,
        ServerCallContext context)
    {
        try
        {
            var snapshot = await runtime.GetSnapshotAsync(GetCancellationToken(context));
            return snapshot.ToProtocol();
        }
        catch (RemoteControlRuntimeException exception)
        {
            throw ToRpcException(exception);
        }
    }

    /// <inheritdoc />
    public override async Task WatchTree(
        WatchTreeRequest request,
        IServerStreamWriter<TreeUpdate> responseStream,
        ServerCallContext context)
    {
        await foreach (var snapshot in runtime.WatchSnapshotsAsync(GetCancellationToken(context)))
        {
            await responseStream.WriteAsync(new TreeUpdate
            {
                Sequence = snapshot.Sequence,
                Snapshot = snapshot.ToProtocol(),
                Reason = "periodic",
            });
        }
    }

    /// <inheritdoc />
    public override async Task WatchFrames(
        WatchFramesRequest request,
        IServerStreamWriter<FrameUpdate> responseStream,
        ServerCallContext context)
    {
        await foreach (var frame in runtime.WatchFramesAsync(GetCancellationToken(context)))
        {
            await responseStream.WriteAsync(frame.ToProtocol());
        }
    }

    /// <inheritdoc />
    public override async Task<CommandResult> InvokeClick(
        InvokeClickRequest request,
        ServerCallContext context)
    {
        var result = await runtime.InvokeClickAsync(
            request.NodeId,
            RemoteControlClientIdentity.From(context),
            GetCancellationToken(context));
        return result.ToProtocol();
    }

    /// <inheritdoc />
    public override async Task<CommandResult> InvokeFocus(
        InvokeFocusRequest request,
        ServerCallContext context)
    {
        var result = await runtime.InvokeFocusAsync(
            request.NodeId,
            RemoteControlClientIdentity.From(context),
            GetCancellationToken(context));
        return result.ToProtocol();
    }

    /// <inheritdoc />
    public override async Task<CommandResult> SetProperty(
        SetPropertyRequest request,
        ServerCallContext context)
    {
        var result = await runtime.SetPropertyAsync(
            request.NodeId,
            request.PropertyName,
            request.Value,
            RemoteControlClientIdentity.From(context),
            GetCancellationToken(context));

        return result.ToProtocol();
    }

    /// <inheritdoc />
    public override async Task<CommandResult> SendInput(
        SendInputRequest request,
        ServerCallContext context)
    {
        var result = await runtime.SendInputAsync(
            request.Events,
            RemoteControlClientIdentity.From(context),
            GetCancellationToken(context));

        return result.ToProtocol();
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

        await foreach (var entry in runtime.WatchLogEntriesAsync(
            minimumLevel,
            categoryPrefix,
            GetCancellationToken(context)))
        {
            await responseStream.WriteAsync(entry.ToProtocol());
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

    private static CancellationToken GetCancellationToken(ServerCallContext? context)
    {
        return context?.CancellationToken ?? CancellationToken.None;
    }

    private static RpcException ToRpcException(RemoteControlRuntimeException exception)
    {
        var statusCode = exception.ErrorCode switch
        {
            RemoteControlRuntimeErrorCode.InvalidArgument => StatusCode.InvalidArgument,
            RemoteControlRuntimeErrorCode.FailedPrecondition => StatusCode.FailedPrecondition,
            RemoteControlRuntimeErrorCode.Cancelled => StatusCode.Cancelled,
            RemoteControlRuntimeErrorCode.Unsupported => StatusCode.Unimplemented,
            _ => StatusCode.Unknown,
        };

        return new RpcException(new Status(statusCode, exception.Message));
    }
}
