using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.RemoteControl.Client.Logging;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Remote log stream tool panel.
/// </summary>
public sealed partial class LogPanel : UserControl
{
    /// <summary>
    /// Defines the <see cref="IsDockHost"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsDockHostProperty =
        AvaloniaProperty.Register<LogPanel, bool>(nameof(IsDockHost), true);

    private RemoteLogViewModel? subscribedViewModel;

    static LogPanel()
    {
        IsDockHostProperty.Changed.AddClassHandler<LogPanel>((panel, _) => panel.UpdatePresentationState());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogPanel"/> class.
    /// </summary>
    public LogPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the panel should float.
    /// </summary>
    public event EventHandler? FloatRequested;

    /// <summary>
    /// Raised when the panel should dock.
    /// </summary>
    public event EventHandler? DockRequested;

    /// <summary>
    /// Gets or sets a value indicating whether this panel is the docked main-shell host.
    /// </summary>
    public bool IsDockHost
    {
        get => GetValue(IsDockHostProperty);
        set => SetValue(IsDockHostProperty, value);
    }

    /// <summary>
    /// Gets or sets the shared log view model.
    /// </summary>
    public RemoteLogViewModel? ViewModel
    {
        get => DataContext as RemoteLogViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Scrolls the list to the latest visible row.
    /// </summary>
    public void ScrollToEnd()
    {
        if (RowsList is null)
        {
            return;
        }

        var count = ViewModel?.Rows.Count ?? 0;
        if (count == 0)
        {
            return;
        }

        RowsList.SelectedIndex = count - 1;
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (subscribedViewModel is { } previous)
        {
            previous.PropertyChanged -= ViewModelPropertyChanged;
            previous.Rows.CollectionChanged -= RowsCollectionChanged;
        }

        subscribedViewModel = DataContext as RemoteLogViewModel;
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.PropertyChanged += ViewModelPropertyChanged;
            subscribedViewModel.Rows.CollectionChanged += RowsCollectionChanged;
        }

        UpdatePresentationState();
        ScrollToEnd();
    }

    private void RowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add)
        {
            ScrollToEnd();
        }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(RemoteLogViewModel.IsPoppedOut), StringComparison.Ordinal))
        {
            UpdatePresentationState();
        }
    }

    private void UpdatePresentationState()
    {
        if (RowsList is null)
        {
            return;
        }

        var showPlaceholder = IsDockHost && ViewModel?.IsPoppedOut == true;
        RowsList.IsVisible = !showPlaceholder;
        RowsList.ItemsSource = showPlaceholder ? null : ViewModel?.Rows;
        FloatingPlaceholder.IsVisible = showPlaceholder;
        FloatButton.IsEnabled = !showPlaceholder;
        FloatButton.IsVisible = IsDockHost;
    }

    private void FloatClicked(object? sender, RoutedEventArgs e)
    {
        FloatRequested?.Invoke(this, EventArgs.Empty);
    }

    private void DockClicked(object? sender, RoutedEventArgs e)
    {
        DockRequested?.Invoke(this, EventArgs.Empty);
    }
}
