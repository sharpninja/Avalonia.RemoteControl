using System.ComponentModel;
using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Live remote UI tool panel.
/// </summary>
public sealed partial class LiveViewPanel : UserControl
{
    private LiveViewPanelViewModel? subscribedViewModel;

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
        set => DataContext = value;
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (subscribedViewModel is { } previous)
        {
            previous.PropertyChanged -= ViewModelPropertyChanged;
        }

        subscribedViewModel = DataContext as LiveViewPanelViewModel;
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        UpdatePlaceholder();
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
        if (Placeholder is null)
        {
            return;
        }

        Placeholder.IsVisible = ViewModel?.HasContent != true;
    }
}
