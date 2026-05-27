using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Right-side remote tools tabbed panel.
/// </summary>
public sealed partial class RemoteToolsPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteToolsPanel"/> class.
    /// </summary>
    public RemoteToolsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the selected tab changes.
    /// </summary>
    public event EventHandler? SelectedTabChanged;

    /// <summary>
    /// Raised when the nested live-view chrome emits a command.
    /// </summary>
    public event EventHandler<DockPaneCommandEventArgs>? LiveViewCommandRequested;

    /// <summary>
    /// Raised when the nested live-view header is dragged.
    /// </summary>
    public event EventHandler<DockPaneDragCompletedEventArgs>? LiveViewHeaderDragCompleted;

    /// <summary>
    /// Gets or sets the panel view model.
    /// </summary>
    public RemoteToolsPanelViewModel? ViewModel
    {
        get => DataContext as RemoteToolsPanelViewModel;
        set
        {
            DataContext = value;
            ActionsPanel.ViewModel = value?.Actions;
            LiveViewPanel.ViewModel = value?.LiveView;
            ProjectPanel.ViewModel = value?.Project;
        }
    }

    /// <summary>
    /// Gets the actions panel.
    /// </summary>
    public ActionsPanel Actions => ActionsPanel;

    /// <summary>
    /// Gets the live-view panel.
    /// </summary>
    public LiveViewPanel LiveView => LiveViewPanel;

    /// <summary>
    /// Gets the project panel.
    /// </summary>
    public ProjectPanel Project => ProjectPanel;

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

    private void LiveViewChromeCommandRequested(object? sender, DockPaneCommandEventArgs e)
    {
        LiveViewCommandRequested?.Invoke(this, e);
    }

    private void LiveViewChromeHeaderDragCompleted(object? sender, DockPaneDragCompletedEventArgs e)
    {
        LiveViewHeaderDragCompleted?.Invoke(this, e);
    }
}
