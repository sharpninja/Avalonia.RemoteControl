using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Project/session/replay tool panel.
/// </summary>
public sealed partial class ProjectPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectPanel"/> class.
    /// </summary>
    public ProjectPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when project save is requested.
    /// </summary>
    public event EventHandler? SaveProjectRequested;

    /// <summary>
    /// Raised when refresh is requested.
    /// </summary>
    public event EventHandler? RefreshRequested;

    /// <summary>
    /// Gets or sets the panel view model.
    /// </summary>
    public ProjectPanelViewModel? ViewModel
    {
        get => DataContext as ProjectPanelViewModel;
        set => DataContext = value;
    }

    private void SaveProjectClicked(object? sender, RoutedEventArgs e)
    {
        SaveProjectRequested?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshClicked(object? sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}
