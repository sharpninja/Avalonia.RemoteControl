using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Remote control tree tool panel.
/// </summary>
public sealed partial class ControlTreePanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ControlTreePanel"/> class.
    /// </summary>
    public ControlTreePanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the selected tree item changes.
    /// </summary>
    public event EventHandler<RemoteTreeItem?>? SelectedItemChanged;

    /// <summary>
    /// Gets or sets the panel view model.
    /// </summary>
    public ControlTreePanelViewModel? ViewModel
    {
        get => DataContext as ControlTreePanelViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Selects a tree item.
    /// </summary>
    /// <param name="item">Tree item to select.</param>
    public void SelectItem(RemoteTreeItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.ExpandAncestors();
        if (ViewModel is { } viewModel)
        {
            viewModel.SelectedItem = item;
        }

        Tree.SelectedItem = item;
    }

    private void TreeSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.AddedItems.OfType<RemoteTreeItem>().FirstOrDefault();
        if (ViewModel is { } viewModel)
        {
            viewModel.SelectedItem = item;
        }

        SelectedItemChanged?.Invoke(this, item);
    }
}
