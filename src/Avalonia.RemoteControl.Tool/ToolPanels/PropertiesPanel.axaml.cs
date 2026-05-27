using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Selected-node properties tool panel.
/// </summary>
public sealed partial class PropertiesPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PropertiesPanel"/> class.
    /// </summary>
    public PropertiesPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when a property row is selected.
    /// </summary>
    public event EventHandler<PropertyRow?>? PropertySelected;

    /// <summary>
    /// Gets or sets the panel view model.
    /// </summary>
    public PropertiesPanelViewModel? ViewModel
    {
        get => DataContext as PropertiesPanelViewModel;
        set => DataContext = value;
    }

    private void RowsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var row = e.AddedItems.OfType<PropertyRow>().FirstOrDefault();
        if (ViewModel is { } viewModel)
        {
            viewModel.SelectedItem = row;
        }

        PropertySelected?.Invoke(this, row);
    }
}
