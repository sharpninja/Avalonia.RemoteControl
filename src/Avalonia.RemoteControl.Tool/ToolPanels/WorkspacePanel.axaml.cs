using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Default fill workspace panel.
/// </summary>
public sealed partial class WorkspacePanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspacePanel"/> class.
    /// </summary>
    public WorkspacePanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the selected workspace tab changes.
    /// </summary>
    public event EventHandler? SelectedTabChanged;

    /// <summary>
    /// Gets or sets the workspace panel view model.
    /// </summary>
    public WorkspacePanelViewModel? ViewModel
    {
        get => DataContext as WorkspacePanelViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Gets the terminal panel.
    /// </summary>
    public TerminalPanel Terminal => TerminalPanel;

    /// <summary>
    /// Gets the properties panel.
    /// </summary>
    public PropertiesPanel Properties => PropertiesPanel;

    /// <summary>
    /// Gets or sets the selected tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => Tabs.SelectedIndex;
        set => Tabs.SelectedIndex = value;
    }

    private void TabsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.SelectedTabIndex = Tabs.SelectedIndex;
        }

        SelectedTabChanged?.Invoke(this, EventArgs.Empty);
    }
}
