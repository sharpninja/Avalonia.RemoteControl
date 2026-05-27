using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Floating host for a dockable tool panel.
/// </summary>
public sealed partial class FloatingDockPaneWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingDockPaneWindow"/> class for XAML tooling.
    /// </summary>
    public FloatingDockPaneWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FloatingDockPaneWindow"/> class.
    /// </summary>
    /// <param name="panelId">Stable panel identifier.</param>
    /// <param name="title">Panel title.</param>
    /// <param name="glyph">Panel glyph.</param>
    /// <param name="body">Panel body control.</param>
    public FloatingDockPaneWindow(string panelId, string title, string glyph, object body)
    {
        InitializeComponent();
        Chrome.PanelId = panelId;
        Chrome.Title = title;
        Chrome.Glyph = glyph;
        Chrome.Body = body;
        Chrome.IsFloating = true;
        Chrome.MoveDragRequested += (_, e) => BeginMoveDrag(e);
    }

    /// <summary>
    /// Raised when the floating chrome emits a command.
    /// </summary>
    public event EventHandler<DockPaneCommandEventArgs>? CommandRequested;

    /// <summary>
    /// Gets the hosted panel id.
    /// </summary>
    public string PanelId => Chrome.PanelId;

    /// <summary>
    /// Releases the hosted body so it can be reattached to the docked shell.
    /// </summary>
    /// <returns>The hosted body, if any.</returns>
    public object? ReleaseBody()
    {
        var body = Chrome.Body;
        Chrome.Body = null;
        return body;
    }

    private void ChromeCommandRequested(object? sender, DockPaneCommandEventArgs e)
    {
        CommandRequested?.Invoke(this, e);
    }
}
