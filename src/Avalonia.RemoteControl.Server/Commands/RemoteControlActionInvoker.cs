using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Server.Commands;

/// <summary>
/// Invokes policy-approved remote control actions.
/// </summary>
public sealed class RemoteControlActionInvoker
{
    private readonly IRemoteControlNodeResolver nodeResolver;
    private readonly AvaloniaRemoteControlOptions options;
    private readonly IRemoteControlDispatcher dispatcher;
    private readonly ILogger<RemoteControlActionInvoker> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlActionInvoker"/> class.
    /// </summary>
    /// <param name="nodeResolver">Resolver for stable node IDs.</param>
    /// <param name="options">Remote-control options.</param>
    /// <param name="dispatcher">Dispatcher used to access controls safely.</param>
    /// <param name="logger">Audit logger.</param>
    public RemoteControlActionInvoker(
        IRemoteControlNodeResolver nodeResolver,
        IOptions<AvaloniaRemoteControlOptions> options,
        IRemoteControlDispatcher dispatcher,
        ILogger<RemoteControlActionInvoker> logger)
    {
        this.nodeResolver = nodeResolver;
        this.options = options.Value;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    /// <summary>
    /// Invokes a basic click action for a stable node ID.
    /// </summary>
    /// <param name="nodeId">The stable node ID.</param>
    /// <returns>The sanitized command result.</returns>
    public ValueTask<RemoteControlCommandResult> InvokeClickAsync(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return dispatcher.InvokeAsync(() => InvokeClick(nodeId));
    }

    /// <summary>
    /// Requests focus for a stable node ID.
    /// </summary>
    /// <param name="nodeId">The stable node ID.</param>
    /// <returns>The sanitized command result.</returns>
    public ValueTask<RemoteControlCommandResult> InvokeFocusAsync(string nodeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        return dispatcher.InvokeAsync(() => InvokeFocus(nodeId));
    }

    private RemoteControlCommandResult InvokeClick(string nodeId)
    {
        if (!options.AllowRemoteActions)
        {
            logger.LogWarning("Remote click rejected because remote actions are disabled for node {NodeId}", nodeId);
            return new RemoteControlCommandResult(false, "Remote actions are disabled by policy.");
        }

        if (!nodeResolver.TryResolve(nodeId, out var control))
        {
            logger.LogWarning("Remote click rejected for stale node {NodeId}", nodeId);
            return new RemoteControlCommandResult(false, "Node is no longer available.");
        }

        if (control is Button button)
        {
            if (button.Command is not null)
            {
                if (!button.Command.CanExecute(button.CommandParameter))
                {
                    return new RemoteControlCommandResult(false, "Button command cannot execute.");
                }

                button.Command.Execute(button.CommandParameter);
            }
            else
            {
                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }

            logger.LogInformation("Remote click succeeded for node {NodeId}", nodeId);
            return new RemoteControlCommandResult(true, "Click invoked.");
        }

        return new RemoteControlCommandResult(false, "Control does not support click invocation.");
    }

    private RemoteControlCommandResult InvokeFocus(string nodeId)
    {
        if (!options.AllowRemoteActions)
        {
            logger.LogWarning("Remote focus rejected because remote actions are disabled for node {NodeId}", nodeId);
            return new RemoteControlCommandResult(false, "Remote actions are disabled by policy.");
        }

        if (!nodeResolver.TryResolve(nodeId, out var control))
        {
            logger.LogWarning("Remote focus rejected for stale node {NodeId}", nodeId);
            return new RemoteControlCommandResult(false, "Node is no longer available.");
        }

        control.Focus();
        logger.LogInformation("Remote focus requested for node {NodeId}", nodeId);
        return new RemoteControlCommandResult(true, "Focus requested.");
    }
}
