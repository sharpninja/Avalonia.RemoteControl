using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Snapshots;
using Microsoft.Extensions.Logging;

namespace Avalonia.RemoteControl.Server.Runtime;

/// <summary>
/// Implements transport-independent remote-control operations.
/// </summary>
public sealed class RemoteControlRuntime : IRemoteControlRuntime
{
    private readonly AvaloniaRemoteControlService remoteControlService;
    private readonly IControlTreeSnapshotProvider snapshotProvider;
    private readonly IRemoteControlRootProvider rootProvider;
    private readonly RemoteControlTreeStreamService treeStreamService;
    private readonly RemoteControlLogStreamService logStreamService;
    private readonly RemoteControlActionInvoker actionInvoker;
    private readonly RemoteControlPropertyMutationService propertyMutationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlRuntime"/> class.
    /// </summary>
    /// <param name="remoteControlService">The remote-control application service.</param>
    /// <param name="snapshotProvider">The read-only tree snapshot provider.</param>
    /// <param name="rootProvider">The host app root provider.</param>
    /// <param name="treeStreamService">The live tree stream service.</param>
    /// <param name="logStreamService">The live log stream service.</param>
    /// <param name="actionInvoker">The remote action invoker.</param>
    /// <param name="propertyMutationService">The remote property mutation service.</param>
    public RemoteControlRuntime(
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
    public RemoteControlCapabilities GetCapabilities()
    {
        return remoteControlService.GetCapabilities();
    }

    /// <inheritdoc />
    public async ValueTask<RemoteControlTreeSnapshot> GetSnapshotAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var root = rootProvider.GetRootControl();

        if (root is null)
        {
            throw new RemoteControlRuntimeException(
                RemoteControlRuntimeErrorCode.FailedPrecondition,
                "No Avalonia remote-control root control is registered.");
        }

        return await snapshotProvider.CaptureSnapshotAsync(root).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<RemoteControlTreeSnapshot> WatchSnapshotsAsync(
        CancellationToken cancellationToken = default)
    {
        return treeStreamService.WatchSnapshotsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<RemoteControlCommandResult> InvokeClickAsync(
        string nodeId,
        string clientIdentity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return actionInvoker.InvokeClickAsync(nodeId, clientIdentity);
    }

    /// <inheritdoc />
    public ValueTask<RemoteControlCommandResult> InvokeFocusAsync(
        string nodeId,
        string clientIdentity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return actionInvoker.InvokeFocusAsync(nodeId, clientIdentity);
    }

    /// <inheritdoc />
    public ValueTask<RemoteControlCommandResult> SetPropertyAsync(
        string nodeId,
        string propertyName,
        string value,
        string clientIdentity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return propertyMutationService.SetPropertyAsync(nodeId, propertyName, value, clientIdentity);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<RemoteControlLogEntry> WatchLogEntriesAsync(
        LogLevel minimumLevel,
        string? categoryPrefix,
        CancellationToken cancellationToken = default)
    {
        return logStreamService.WatchEntriesAsync(minimumLevel, categoryPrefix, cancellationToken);
    }
}
