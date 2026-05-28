using System.ComponentModel;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Tool;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlPropertyGridTests
{
    [Fact]
    public void PropertyGridObjectExposesRemotePropertiesAsDescriptors()
    {
        var viewModel = new PropertiesPanelViewModel();
        viewModel.ShowNode(CreateNode());

        var properties = TypeDescriptor.GetProperties(viewModel.GridObject!);

        var text = properties.Find("Text", false);
        var bounds = properties.Find("Bounds", false);
        var token = properties.Find("AuthToken", false);

        Assert.NotNull(viewModel.GridObject);
        Assert.Equal(3, properties.Count);
        Assert.NotNull(text);
        Assert.False(text!.IsReadOnly);
        Assert.Equal("Before", text.GetValue(viewModel.GridObject));
        Assert.Equal("Text", text.DisplayName);
        Assert.Equal("Avalonia.Controls.TextBlock", text.Category);
        Assert.NotNull(bounds);
        Assert.True(bounds!.IsReadOnly);
        Assert.NotNull(token);
        Assert.True(token!.IsReadOnly);
    }

    [Fact]
    public void WritableDescriptorRaisesRemoteEditRequestAndUpdatesRow()
    {
        var viewModel = new PropertiesPanelViewModel();
        RemotePropertyEditRequestedEventArgs? edit = null;
        viewModel.PropertyEditRequested += (_, args) => edit = args;
        viewModel.ShowNode(CreateNode());
        var descriptor = TypeDescriptor.GetProperties(viewModel.GridObject!).Find("Text", false)!;

        descriptor.SetValue(viewModel.GridObject, "After");

        Assert.NotNull(edit);
        Assert.Equal("Text", edit!.Row.Name);
        Assert.Equal("After", edit.Row.Value);
        Assert.Equal("After", viewModel.Rows.Single(row => row.Name == "Text").Value);
        Assert.Equal("After", descriptor.GetValue(viewModel.GridObject));
    }

    [Fact]
    public void EnumDescriptorExposesEnumTypeForDropdownEditors()
    {
        var viewModel = new PropertiesPanelViewModel();
        RemotePropertyEditRequestedEventArgs? edit = null;
        viewModel.PropertyEditRequested += (_, args) => edit = args;
        var node = CreateNode();
        node.Properties.Add(new PropertyValue
        {
            Name = "HorizontalAlignment",
            DeclaringType = "Avalonia.Layout.Layoutable",
            Value = "Stretch",
            ValueType = "HorizontalAlignment",
            CanWrite = true,
            IsEnum = true,
            EnumValues = { "Stretch", "Left", "Center", "Right" },
        });
        viewModel.ShowNode(node);
        var descriptor = TypeDescriptor.GetProperties(viewModel.GridObject!).Find("HorizontalAlignment", false)!;

        Assert.True(descriptor.PropertyType.IsEnum);
        Assert.Equal(new[] { "Stretch", "Left", "Center", "Right" }, Enum.GetNames(descriptor.PropertyType));
        Assert.Equal("Stretch", descriptor.GetValue(viewModel.GridObject)!.ToString());

        descriptor.SetValue(viewModel.GridObject, Enum.Parse(descriptor.PropertyType, "Center"));

        Assert.NotNull(edit);
        Assert.Equal("HorizontalAlignment", edit!.Row.Name);
        Assert.Equal("Center", edit.Row.Value);
        Assert.Equal("Center", descriptor.GetValue(viewModel.GridObject)!.ToString());
    }

    [Fact]
    public void DuplicatePropertyNamesGetStableDescriptorNames()
    {
        var viewModel = new PropertiesPanelViewModel();
        var node = new TreeNode
        {
            Id = "node-duplicate",
            TypeName = "DuplicateControl",
        };
        node.Properties.Add(new PropertyValue
        {
            Name = "Content",
            DeclaringType = "Avalonia.Controls.ContentControl",
            Value = "Outer",
            ValueType = "System.Object",
            CanWrite = true,
        });
        node.Properties.Add(new PropertyValue
        {
            Name = "Content",
            DeclaringType = "FunWasHad.Controls.Card",
            Value = "Inner",
            ValueType = "System.String",
            CanWrite = true,
        });

        viewModel.ShowNode(node);
        var properties = TypeDescriptor.GetProperties(viewModel.GridObject!);

        Assert.NotNull(properties.Find("Content", false));
        Assert.NotNull(properties.Find("Content [2]", false));
        Assert.Equal("Outer", properties.Find("Content", false)!.GetValue(viewModel.GridObject));
        Assert.Equal("Inner", properties.Find("Content [2]", false)!.GetValue(viewModel.GridObject));
        Assert.Equal(
            "FunWasHad.Controls.Card",
            viewModel.SelectProperty("Content [2]")!.DeclaringType);
    }

    [Fact]
    public void ShowNodeNullClearsPropertyGridState()
    {
        var viewModel = new PropertiesPanelViewModel();
        viewModel.ShowNode(CreateNode());

        viewModel.ShowNode(null);

        Assert.Null(viewModel.GridObject);
        Assert.Empty(viewModel.Rows);
        Assert.Null(viewModel.SelectedItem);
    }

    private static TreeNode CreateNode()
    {
        var node = new TreeNode
        {
            Id = "node-1",
            TypeName = "TextBlock",
        };
        node.Properties.Add(new PropertyValue
        {
            Name = "Text",
            DeclaringType = "Avalonia.Controls.TextBlock",
            Value = "Before",
            ValueType = "System.String",
            CanWrite = true,
        });
        node.Properties.Add(new PropertyValue
        {
            Name = "Bounds",
            DeclaringType = "Avalonia.Layout.Layoutable",
            Value = "0,0,100,20",
            ValueType = "Avalonia.Rect",
            CanWrite = false,
        });
        node.Properties.Add(new PropertyValue
        {
            Name = "AuthToken",
            DeclaringType = "FunWasHad.Shell",
            Value = "***",
            ValueType = "System.String",
            CanWrite = true,
            IsRedacted = true,
        });
        return node;
    }
}
