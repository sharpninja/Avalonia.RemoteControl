using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Remote command action tool panel.
/// </summary>
public sealed partial class ActionsPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActionsPanel"/> class.
    /// </summary>
    public ActionsPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when click invocation is requested.
    /// </summary>
    public event EventHandler? InvokeClickRequested;

    /// <summary>
    /// Raised when focus invocation is requested.
    /// </summary>
    public event EventHandler? FocusRequested;

    /// <summary>
    /// Raised when property mutation is requested.
    /// </summary>
    public event EventHandler? SetPropertyRequested;

    /// <summary>
    /// Gets or sets the panel view model.
    /// </summary>
    public ActionsPanelViewModel? ViewModel
    {
        get => DataContext as ActionsPanelViewModel;
        set => DataContext = value;
    }

    /// <summary>
    /// Updates the property editor fields.
    /// </summary>
    /// <param name="name">Property name.</param>
    /// <param name="value">Property value.</param>
    public void SetPropertyEditor(string name, string value)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.PropertyName = name;
            viewModel.PropertyValue = value;
        }
    }

    private void InvokeClickClicked(object? sender, RoutedEventArgs e)
    {
        InvokeClickRequested?.Invoke(this, EventArgs.Empty);
    }

    private void FocusClicked(object? sender, RoutedEventArgs e)
    {
        FocusRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetPropertyClicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.PropertyName = PropertyNameBox.Text ?? string.Empty;
            viewModel.PropertyValue = PropertyValueBox.Text ?? string.Empty;
        }

        SetPropertyRequested?.Invoke(this, EventArgs.Empty);
    }
}
