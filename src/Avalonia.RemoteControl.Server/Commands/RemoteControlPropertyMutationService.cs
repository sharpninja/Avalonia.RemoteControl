using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.RemoteControl.Server.Snapshots;
using Avalonia.RemoteControl.Server.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Server.Commands;

/// <summary>
/// Applies safe, policy-approved property mutations to live controls.
/// </summary>
public sealed class RemoteControlPropertyMutationService
{
    private readonly IRemoteControlNodeResolver nodeResolver;
    private readonly AvaloniaRemoteControlOptions options;
    private readonly IRemoteControlDispatcher dispatcher;
    private readonly ILogger<RemoteControlPropertyMutationService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlPropertyMutationService"/> class.
    /// </summary>
    /// <param name="nodeResolver">Resolver for stable node IDs.</param>
    /// <param name="options">Remote-control options.</param>
    /// <param name="dispatcher">Dispatcher used to access controls safely.</param>
    /// <param name="logger">Audit logger.</param>
    public RemoteControlPropertyMutationService(
        IRemoteControlNodeResolver nodeResolver,
        IOptions<AvaloniaRemoteControlOptions> options,
        IRemoteControlDispatcher dispatcher,
        ILogger<RemoteControlPropertyMutationService> logger)
    {
        this.nodeResolver = nodeResolver;
        this.options = options.Value;
        this.dispatcher = dispatcher;
        this.logger = logger;
    }

    /// <summary>
    /// Sets a policy-approved public property on a live control.
    /// </summary>
    /// <param name="nodeId">The stable node ID.</param>
    /// <param name="propertyName">The public property name.</param>
    /// <param name="value">The requested string value.</param>
    /// <returns>The sanitized command result.</returns>
    public ValueTask<RemoteControlCommandResult> SetPropertyAsync(
        string nodeId,
        string propertyName,
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        return dispatcher.InvokeAsync(() => SetProperty(nodeId, propertyName, value));
    }

    private RemoteControlCommandResult SetProperty(string nodeId, string propertyName, string value)
    {
        if (!nodeResolver.TryResolve(nodeId, out var control))
        {
            logger.LogWarning("Remote property mutation rejected for stale node {NodeId}", nodeId);
            return new RemoteControlCommandResult(false, "Node is no longer available.");
        }

        if (IsSensitive(propertyName))
        {
            logger.LogWarning(
                "Remote property mutation blocked for sensitive property {PropertyName} on node {NodeId}",
                propertyName,
                nodeId);
            return new RemoteControlCommandResult(false, "Property is blocked by redaction policy.");
        }

        if (!IsPropertyAllowed(control.GetType(), propertyName))
        {
            logger.LogWarning(
                "Remote property mutation denied by policy for {ControlType}.{PropertyName} on node {NodeId}",
                control.GetType().Name,
                propertyName,
                nodeId);
            return new RemoteControlCommandResult(false, "Property mutation is not allowed by policy.");
        }

        var property = control.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

        if (property is null || property.SetMethod is not { IsPublic: true } || property.GetIndexParameters().Length != 0)
        {
            return new RemoteControlCommandResult(false, "Property is not publicly writable.");
        }

        if (!TryConvert(value, property.PropertyType, out var convertedValue))
        {
            return new RemoteControlCommandResult(false, "Value cannot be converted to the target property type.");
        }

        try
        {
            property.SetValue(control, convertedValue);
            logger.LogInformation(
                "Remote property mutation succeeded for {ControlType}.{PropertyName} on node {NodeId}",
                control.GetType().Name,
                propertyName,
                nodeId);

            return new RemoteControlCommandResult(true, "Property updated.");
        }
        catch (Exception ex) when (ex is ArgumentException or TargetInvocationException or MethodAccessException)
        {
            logger.LogWarning(
                "Remote property mutation failed for {ControlType}.{PropertyName} on node {NodeId}",
                control.GetType().Name,
                propertyName,
                nodeId);

            return new RemoteControlCommandResult(false, "Property update failed.");
        }
    }

    private bool IsPropertyAllowed(Type controlType, string propertyName)
    {
        if (!options.DenyPropertyMutationByDefault)
        {
            return true;
        }

        return options.AllowedMutableProperties.Contains(propertyName)
            || options.AllowedMutableProperties.Contains($"{controlType.Name}.{propertyName}")
            || options.AllowedMutableProperties.Contains($"{controlType.FullName}.{propertyName}");
    }

    private bool IsSensitive(string propertyName)
    {
        return options.SensitiveNameFragments.Any(fragment =>
            propertyName.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryConvert(string value, Type targetType, out object? convertedValue)
    {
        var actualType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (string.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(targetType) is not null)
        {
            convertedValue = null;
            return true;
        }

        try
        {
            if (actualType == typeof(string))
            {
                convertedValue = value;
                return true;
            }

            if (actualType == typeof(bool))
            {
                convertedValue = bool.Parse(value);
                return true;
            }

            if (actualType.IsEnum)
            {
                convertedValue = Enum.Parse(actualType, value, ignoreCase: true);
                return true;
            }

            if (actualType == typeof(Guid))
            {
                convertedValue = Guid.Parse(value);
                return true;
            }

            convertedValue = Convert.ChangeType(value, actualType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or ArgumentException or OverflowException)
        {
            convertedValue = null;
            return false;
        }
    }
}
