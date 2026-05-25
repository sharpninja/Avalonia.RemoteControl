using System.Globalization;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Snapshots;
using GrpcLogEntry = Avalonia.RemoteControl.Protocol.V1.LogEntry;
using GrpcRect = Avalonia.RemoteControl.Protocol.V1.Rect;

namespace Avalonia.RemoteControl.Server.Grpc;

internal static class RemoteControlGrpcMappers
{
    public static GetCapabilitiesResponse ToGrpc(this RemoteControlCapabilities capabilities)
    {
        return new GetCapabilitiesResponse
        {
            ProtocolVersion = capabilities.ProtocolVersion,
            SupportsTreeSnapshots = capabilities.SupportsTreeSnapshots,
            SupportsTreeStreaming = capabilities.SupportsTreeStreaming,
            SupportsClickInvocation = capabilities.SupportsClickInvocation,
            SupportsPropertyMutation = capabilities.SupportsPropertyMutation,
            SupportsLogStreaming = capabilities.SupportsLogStreaming,
        };
    }

    public static TreeSnapshot ToGrpc(this RemoteControlTreeSnapshot snapshot)
    {
        var response = new TreeSnapshot
        {
            Sequence = snapshot.Sequence,
        };

        response.Nodes.AddRange(snapshot.Nodes.Select(ToGrpc));

        return response;
    }

    public static CommandResult ToGrpc(this RemoteControlCommandResult result)
    {
        return new CommandResult
        {
            Succeeded = result.Succeeded,
            Message = result.Message,
        };
    }

    public static GrpcLogEntry ToGrpc(this RemoteControlLogEntry entry)
    {
        return new GrpcLogEntry
        {
            Sequence = entry.Sequence,
            TimestampUtc = entry.TimestampUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
            Level = entry.Level.ToString(),
            Category = entry.Category,
            EventId = entry.EventId,
            Message = entry.Message,
            ExceptionSummary = entry.ExceptionSummary,
            DroppedCount = entry.DroppedCount,
            StructuredState = entry.StructuredState,
            ScopeSummary = entry.ScopeSummary,
        };
    }

    private static TreeNode ToGrpc(RemoteControlNodeSnapshot node)
    {
        var response = new TreeNode
        {
            Id = node.Id,
            ParentId = node.ParentId ?? string.Empty,
            TypeName = node.TypeName,
            Name = node.Name ?? string.Empty,
            AutomationName = node.AutomationName ?? string.Empty,
            AutomationId = node.AutomationId ?? string.Empty,
            Bounds = new GrpcRect
            {
                X = node.Bounds.X,
                Y = node.Bounds.Y,
                Width = node.Bounds.Width,
                Height = node.Bounds.Height,
            },
            IsVisible = node.IsVisible,
            IsEnabled = node.IsEnabled,
            IsFocused = node.IsFocused,
        };

        response.Classes.AddRange(node.Classes);
        response.Properties.AddRange(node.Properties.Select(ToGrpc));

        return response;
    }

    private static PropertyValue ToGrpc(RemoteControlPropertySnapshot property)
    {
        return new PropertyValue
        {
            Name = property.Name,
            DeclaringType = property.DeclaringType,
            Value = property.Value,
            ValueType = property.ValueType,
            CanWrite = property.CanWrite,
            IsRedacted = property.IsRedacted,
        };
    }
}
