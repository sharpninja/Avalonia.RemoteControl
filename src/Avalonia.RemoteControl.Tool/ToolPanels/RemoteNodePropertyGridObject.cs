using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Dynamic property-grid source for the currently selected remote node.
/// </summary>
public sealed class RemoteNodePropertyGridObject : ICustomTypeDescriptor, INotifyPropertyChanged
{
    private readonly Dictionary<string, RemoteNodePropertyGridEntry> entriesByDescriptorName;
    private readonly PropertyDescriptorCollection properties;
    private readonly Action<RemoteNodePropertyGridEntry, string> valueSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteNodePropertyGridObject"/> class.
    /// </summary>
    /// <param name="nodeId">Selected remote node id.</param>
    /// <param name="entries">Remote property entries.</param>
    /// <param name="valueSet">Callback invoked when a writable property value changes.</param>
    public RemoteNodePropertyGridObject(
        string nodeId,
        IEnumerable<RemoteNodePropertyGridEntry> entries,
        Action<RemoteNodePropertyGridEntry, string> valueSet)
    {
        NodeId = nodeId;
        this.valueSet = valueSet;
        var descriptors = new List<PropertyDescriptor>();
        entriesByDescriptorName = [];
        var nameCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            var descriptorName = CreateDescriptorName(entry, nameCounts);
            entry.DescriptorName = descriptorName;
            entriesByDescriptorName.Add(descriptorName, entry);
            descriptors.Add(new RemotePropertyDescriptor(entry, descriptorName, SetValue));
        }

        properties = new PropertyDescriptorCollection([.. descriptors], true);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the selected remote node id.
    /// </summary>
    public string NodeId { get; }

    /// <summary>
    /// Finds a property entry by the descriptor name used by the property grid.
    /// </summary>
    /// <param name="descriptorName">Descriptor name.</param>
    /// <returns>The matching entry, or <see langword="null"/>.</returns>
    public RemoteNodePropertyGridEntry? FindEntry(string descriptorName)
    {
        return entriesByDescriptorName.TryGetValue(descriptorName, out var entry) ? entry : null;
    }

    /// <inheritdoc />
    public AttributeCollection GetAttributes()
    {
        return AttributeCollection.Empty;
    }

    /// <inheritdoc />
    public string? GetClassName()
    {
        return "Remote Node";
    }

    /// <inheritdoc />
    public string? GetComponentName()
    {
        return NodeId;
    }

    /// <inheritdoc />
    public TypeConverter? GetConverter()
    {
        return null;
    }

    /// <inheritdoc />
    public EventDescriptor? GetDefaultEvent()
    {
        return null;
    }

    /// <inheritdoc />
    public PropertyDescriptor? GetDefaultProperty()
    {
        return null;
    }

    /// <inheritdoc />
    public object? GetEditor(Type editorBaseType)
    {
        return null;
    }

    /// <inheritdoc />
    public EventDescriptorCollection GetEvents(Attribute[]? attributes)
    {
        return EventDescriptorCollection.Empty;
    }

    /// <inheritdoc />
    public EventDescriptorCollection GetEvents()
    {
        return EventDescriptorCollection.Empty;
    }

    /// <inheritdoc />
    public PropertyDescriptorCollection GetProperties(Attribute[]? attributes)
    {
        return properties;
    }

    /// <inheritdoc />
    public PropertyDescriptorCollection GetProperties()
    {
        return properties;
    }

    /// <inheritdoc />
    public object GetPropertyOwner(PropertyDescriptor? pd)
    {
        return this;
    }

    private static string CreateDescriptorName(RemoteNodePropertyGridEntry entry, Dictionary<string, int> nameCounts)
    {
        var baseName = string.IsNullOrWhiteSpace(entry.Name) ? "(unnamed)" : entry.Name;
        nameCounts.TryGetValue(baseName, out var count);
        count++;
        nameCounts[baseName] = count;
        return count == 1 ? baseName : $"{baseName} [{count}]";
    }

    private void SetValue(RemoteNodePropertyGridEntry entry, string value)
    {
        entry.Value = value;
        valueSet(entry, value);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(entry.DescriptorName));
    }

    private sealed class RemotePropertyDescriptor : PropertyDescriptor
    {
        private readonly RemoteNodePropertyGridEntry entry;
        private readonly Action<RemoteNodePropertyGridEntry, string> valueSet;

        public RemotePropertyDescriptor(
            RemoteNodePropertyGridEntry entry,
            string descriptorName,
            Action<RemoteNodePropertyGridEntry, string> valueSet)
            : base(descriptorName, CreateAttributes(entry))
        {
            this.entry = entry;
            this.valueSet = valueSet;
        }

        public override Type ComponentType => typeof(RemoteNodePropertyGridObject);

        public override bool IsReadOnly => entry.IsReadOnly;

        public override Type PropertyType => entry.PropertyType;

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override object? GetValue(object? component)
        {
            return entry.GetEditorValue();
        }

        public override void ResetValue(object component)
        {
        }

        public override void SetValue(object? component, object? value)
        {
            if (IsReadOnly)
            {
                return;
            }

            valueSet(entry, entry.ToRemoteValue(value));
        }

        public override bool ShouldSerializeValue(object component)
        {
            return false;
        }

        private static Attribute[] CreateAttributes(RemoteNodePropertyGridEntry entry)
        {
            var category = string.IsNullOrWhiteSpace(entry.DeclaringType) ? "Properties" : entry.DeclaringType;
            var description = string.IsNullOrWhiteSpace(entry.ValueType)
                ? entry.Name
                : $"{entry.Name} ({entry.ValueType})";
            return
            [
                new CategoryAttribute(category),
                new DisplayNameAttribute(entry.Name),
                new DescriptionAttribute(description),
                new ReadOnlyAttribute(entry.IsReadOnly),
            ];
        }
    }
}

/// <summary>
/// Remote node property entry shown by <see cref="RemoteNodePropertyGridObject"/>.
/// </summary>
public sealed class RemoteNodePropertyGridEntry
{
    private RemoteNodePropertyGridEntry(
        string name,
        string declaringType,
        string value,
        string valueType,
        bool canWrite,
        bool isRedacted,
        bool isEnum,
        IReadOnlyList<string> enumValues)
    {
        Name = name;
        DeclaringType = declaringType;
        Value = value;
        ValueType = valueType;
        CanWrite = canWrite;
        IsRedacted = isRedacted;
        IsEnum = isEnum;
        EnumValues = enumValues;
        enumType = isEnum && enumValues.Count > 0
            ? RemoteEnumPropertyTypeCache.GetOrCreate(valueType, enumValues)
            : null;
    }

    private readonly Type? enumType;

    /// <summary>
    /// Gets the remote property name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the declaring type reported by the remote runtime.
    /// </summary>
    public string DeclaringType { get; }

    /// <summary>
    /// Gets or sets the string value reported by the remote runtime.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets the value type reported by the remote runtime.
    /// </summary>
    public string ValueType { get; }

    /// <summary>
    /// Gets a value indicating whether the remote runtime reported the property as writable.
    /// </summary>
    public bool CanWrite { get; }

    /// <summary>
    /// Gets a value indicating whether the remote runtime redacted the property value.
    /// </summary>
    public bool IsRedacted { get; }

    /// <summary>
    /// Gets a value indicating whether this property is an enum.
    /// </summary>
    public bool IsEnum { get; }

    /// <summary>
    /// Gets the reported enum values.
    /// </summary>
    public IReadOnlyList<string> EnumValues { get; }

    /// <summary>
    /// Gets a value indicating whether this property should be readonly in the property grid.
    /// </summary>
    public bool IsReadOnly => !CanWrite || IsRedacted;

    /// <summary>
    /// Gets the unique descriptor name used by the property grid.
    /// </summary>
    public string DescriptorName { get; internal set; } = string.Empty;

    /// <summary>
    /// Gets the type exposed to the property grid.
    /// </summary>
    public Type PropertyType => enumType ?? typeof(string);

    /// <summary>
    /// Converts the remote string value into the value consumed by the property grid editor.
    /// </summary>
    /// <returns>The editor value.</returns>
    public object? GetEditorValue()
    {
        if (enumType is null)
        {
            return Value;
        }

        if (Enum.TryParse(enumType, Value, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        var values = Enum.GetValues(enumType);
        return values.Length > 0 ? values.GetValue(0) : null;
    }

    /// <summary>
    /// Converts a property-grid editor value into the string value sent to the remote runtime.
    /// </summary>
    /// <param name="value">Editor value.</param>
    /// <returns>Remote string value.</returns>
    public string ToRemoteValue(object? value)
    {
        if (enumType is not null)
        {
            return value?.ToString() ?? string.Empty;
        }

        return value?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Creates a property-grid entry from a protocol property value.
    /// </summary>
    /// <param name="property">Protocol property value.</param>
    /// <returns>A property-grid entry.</returns>
    public static RemoteNodePropertyGridEntry FromPropertyValue(PropertyValue property)
    {
        return new RemoteNodePropertyGridEntry(
            property.Name,
            property.DeclaringType,
            property.Value,
            property.ValueType,
            property.CanWrite,
            property.IsRedacted,
            property.IsEnum,
            [.. property.EnumValues]);
    }
}

internal static class RemoteEnumPropertyTypeCache
{
    private static readonly AssemblyBuilder Assembly =
        AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Avalonia.RemoteControl.Tool.RemoteEnums"), AssemblyBuilderAccess.Run);
    private static readonly ModuleBuilder Module = Assembly.DefineDynamicModule("RemoteEnums");
    private static readonly object Gate = new();
    private static readonly Dictionary<string, Type> Types = [];
    private static int sequence;

    public static Type GetOrCreate(string typeName, IReadOnlyList<string> values)
    {
        var key = typeName + "\u001f" + string.Join("\u001e", values);
        lock (Gate)
        {
            if (Types.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var enumName = "Avalonia.RemoteControl.Tool.RemoteEnums."
                + SanitizeIdentifier(typeName, "RemoteEnum")
                + "_"
                + sequence++;
            var builder = Module.DefineEnum(enumName, TypeAttributes.Public, typeof(int));
            var memberNames = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < values.Count; index++)
            {
                var memberName = SanitizeIdentifier(values[index], "Value" + index);
                if (!memberNames.Add(memberName))
                {
                    memberName = memberName + "_" + index;
                    memberNames.Add(memberName);
                }

                builder.DefineLiteral(memberName, index);
            }

            var type = builder.CreateTypeInfo()!.AsType();
            Types.Add(key, type);
            return type;
        }
    }

    private static string SanitizeIdentifier(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var builder = new StringBuilder(value.Length + 1);
        foreach (var character in value)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        if (builder.Length == 0)
        {
            return fallback;
        }

        if (!char.IsLetter(builder[0]) && builder[0] != '_')
        {
            builder.Insert(0, '_');
        }

        return builder.ToString();
    }
}

/// <summary>
/// Event args for property edits requested through the property grid.
/// </summary>
public sealed class RemotePropertyEditRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemotePropertyEditRequestedEventArgs"/> class.
    /// </summary>
    /// <param name="row">Edited property row.</param>
    public RemotePropertyEditRequestedEventArgs(PropertyRow row)
    {
        Row = row;
    }

    /// <summary>
    /// Gets the edited property row.
    /// </summary>
    public PropertyRow Row { get; }
}
