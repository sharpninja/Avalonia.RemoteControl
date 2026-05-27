using System.ComponentModel;
using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Live remote UI tool panel.
/// </summary>
public sealed partial class LiveViewPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LiveViewPanel"/> class.
    /// </summary>
    public LiveViewPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the panel view model.
    /// </summary>
    public LiveViewPanelViewModel? ViewModel
    {
        get => DataContext as LiveViewPanelViewModel;
        set
        {
            if (ViewModel is { } previous)
            {
                previous.PropertyChanged -= ViewModelPropertyChanged;
            }

            DataContext = value;
            if (value is not null)
            {
                value.PropertyChanged += ViewModelPropertyChanged;
            }

            UpdatePlaceholder();
        }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(LiveViewPanelViewModel.Content), StringComparison.Ordinal) ||
            string.Equals(e.PropertyName, nameof(LiveViewPanelViewModel.HasContent), StringComparison.Ordinal))
        {
            UpdatePlaceholder();
        }
    }

    private void UpdatePlaceholder()
    {
        Placeholder.IsVisible = ViewModel?.HasContent != true;
    }
}
