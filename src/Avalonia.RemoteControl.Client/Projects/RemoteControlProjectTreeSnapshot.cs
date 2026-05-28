using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Persisted control-tree snapshot used by replay artifacts and diffs.
/// </summary>
public sealed class RemoteControlProjectTreeSnapshot
{
    /// <summary>
    /// Gets or sets the remote snapshot sequence number.
    /// </summary>
    public ulong Sequence { get; set; }

    /// <summary>
    /// Gets captured nodes.
    /// </summary>
    public List<RemoteControlProjectTreeNode> Nodes { get; set; } = [];

    /// <summary>
    /// Converts a protocol tree snapshot into a replay-persistable snapshot.
    /// </summary>
    /// <param name="snapshot">Protocol snapshot.</param>
    /// <returns>A project tree snapshot.</returns>
    public static RemoteControlProjectTreeSnapshot FromProtocol(TreeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var result = new RemoteControlProjectTreeSnapshot
        {
            Sequence = snapshot.Sequence,
        };

        foreach (var node in snapshot.Nodes)
        {
            result.Nodes.Add(RemoteControlProjectTreeNode.FromProtocol(node));
        }

        return result;
    }
}

/// <summary>
/// Persisted control-tree node used by replay artifacts and diffs.
/// </summary>
public sealed record RemoteControlProjectTreeNode
{
    /// <summary>
    /// Gets or sets the remote node identifier.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the parent node identifier.
    /// </summary>
    public string ParentId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the control type name.
    /// </summary>
    public string TypeName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the control name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the automation identifier.
    /// </summary>
    public string AutomationId { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the automation name.
    /// </summary>
    public string AutomationName { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the node is visible.
    /// </summary>
    public bool IsVisible { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is enabled.
    /// </summary>
    public bool IsEnabled { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is focused.
    /// </summary>
    public bool IsFocused { get; init; }

    /// <summary>
    /// Gets or sets local bounds.
    /// </summary>
    public RemoteControlProjectRect Bounds { get; init; } = new();

    /// <summary>
    /// Gets or sets root-relative absolute bounds.
    /// </summary>
    public RemoteControlProjectRect AbsoluteBounds { get; init; } = new();

    /// <summary>
    /// Gets CSS/style classes recorded for the node.
    /// </summary>
    public List<string> Classes { get; init; } = [];

    /// <summary>
    /// Gets property values recorded for the node.
    /// </summary>
    public List<RemoteControlProjectPropertyValue> Properties { get; init; } = [];

    /// <summary>
    /// Converts a protocol tree node into a project tree node.
    /// </summary>
    /// <param name="node">Protocol tree node.</param>
    /// <returns>A project tree node.</returns>
    public static RemoteControlProjectTreeNode FromProtocol(TreeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new RemoteControlProjectTreeNode
        {
            Id = node.Id,
            ParentId = node.ParentId,
            TypeName = node.TypeName,
            Name = node.Name,
            AutomationId = node.AutomationId,
            AutomationName = node.AutomationName,
            IsVisible = node.IsVisible,
            IsEnabled = node.IsEnabled,
            IsFocused = node.IsFocused,
            Bounds = RemoteControlProjectRect.FromProtocol(node.Bounds),
            AbsoluteBounds = RemoteControlProjectRect.FromProtocol(node.AbsoluteBounds),
            Classes = node.Classes.ToList(),
            Properties = node.Properties.Select(RemoteControlProjectPropertyValue.FromProtocol).ToList(),
        };
    }
}

/// <summary>
/// Persisted rectangle value.
/// </summary>
public sealed record RemoteControlProjectRect
{
    /// <summary>
    /// Gets or sets the X coordinate.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets or sets the Y coordinate.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets or sets the width.
    /// </summary>
    public double Width { get; init; }

    /// <summary>
    /// Gets or sets the height.
    /// </summary>
    public double Height { get; init; }

    /// <summary>
    /// Converts a protocol rectangle to a project rectangle.
    /// </summary>
    /// <param name="rect">Protocol rectangle.</param>
    /// <returns>A project rectangle.</returns>
    public static RemoteControlProjectRect FromProtocol(Rect? rect)
    {
        return rect is null
            ? new RemoteControlProjectRect()
            : new RemoteControlProjectRect
            {
                X = rect.X,
                Y = rect.Y,
                Width = rect.Width,
                Height = rect.Height,
            };
    }
}

/// <summary>
/// Persisted node property value.
/// </summary>
public sealed record RemoteControlProjectPropertyValue
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the declaring type name.
    /// </summary>
    public string DeclaringType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the string value.
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the value type.
    /// </summary>
    public string ValueType { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the property can be written.
    /// </summary>
    public bool CanWrite { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the property value is redacted.
    /// </summary>
    public bool IsRedacted { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the property value type is an enum.
    /// </summary>
    public bool IsEnum { get; init; }

    /// <summary>
    /// Gets or sets the enum values reported by the remote runtime.
    /// </summary>
    public List<string> EnumValues { get; init; } = [];

    /// <summary>
    /// Converts a protocol property value to a project property value.
    /// </summary>
    /// <param name="property">Protocol property value.</param>
    /// <returns>A project property value.</returns>
    public static RemoteControlProjectPropertyValue FromProtocol(PropertyValue property)
    {
        ArgumentNullException.ThrowIfNull(property);

        return new RemoteControlProjectPropertyValue
        {
            Name = property.Name,
            DeclaringType = property.DeclaringType,
            Value = property.Value,
            ValueType = property.ValueType,
            CanWrite = property.CanWrite,
            IsRedacted = property.IsRedacted,
            IsEnum = property.IsEnum,
            EnumValues = [.. property.EnumValues],
        };
    }
}
