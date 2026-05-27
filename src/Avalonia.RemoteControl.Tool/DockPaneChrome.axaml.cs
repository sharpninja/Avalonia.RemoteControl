using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Visual Studio-like chrome for a dockable tool panel.
/// </summary>
public sealed partial class DockPaneChrome : UserControl
{
    /// <summary>
    /// Defines the <see cref="PanelId"/> property.
    /// </summary>
    public static readonly StyledProperty<string> PanelIdProperty =
        AvaloniaProperty.Register<DockPaneChrome, string>(nameof(PanelId), string.Empty);

    /// <summary>
    /// Defines the <see cref="Title"/> property.
    /// </summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<DockPaneChrome, string>(nameof(Title), "Tool Window");

    /// <summary>
    /// Defines the <see cref="Glyph"/> property.
    /// </summary>
    public static readonly StyledProperty<string> GlyphProperty =
        AvaloniaProperty.Register<DockPaneChrome, string>(nameof(Glyph), "\uE8B7");

    /// <summary>
    /// Defines the <see cref="Body"/> property.
    /// </summary>
    public static readonly StyledProperty<object?> BodyProperty =
        AvaloniaProperty.Register<DockPaneChrome, object?>(nameof(Body));

    /// <summary>
    /// Defines the <see cref="IsFloating"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsFloatingProperty =
        AvaloniaProperty.Register<DockPaneChrome, bool>(nameof(IsFloating));

    /// <summary>
    /// Defines the <see cref="IsAutoHidden"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsAutoHiddenProperty =
        AvaloniaProperty.Register<DockPaneChrome, bool>(nameof(IsAutoHidden));

    /// <summary>
    /// Defines the <see cref="CanFloat"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanFloatProperty =
        AvaloniaProperty.Register<DockPaneChrome, bool>(nameof(CanFloat), true);

    /// <summary>
    /// Defines the <see cref="CanDock"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanDockProperty =
        AvaloniaProperty.Register<DockPaneChrome, bool>(nameof(CanDock), true);

    /// <summary>
    /// Defines the <see cref="CanAutoHide"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanAutoHideProperty =
        AvaloniaProperty.Register<DockPaneChrome, bool>(nameof(CanAutoHide), true);

    private Point? headerDragStart;
    private bool isHeaderDragging;
    private bool namedControlsLoaded;

    static DockPaneChrome()
    {
        TitleProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        GlyphProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        BodyProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        IsFloatingProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        IsAutoHiddenProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        CanFloatProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        CanDockProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
        CanAutoHideProperty.Changed.AddClassHandler<DockPaneChrome>((control, _) => control.UpdateVisualState());
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DockPaneChrome"/> class.
    /// </summary>
    public DockPaneChrome()
    {
        InitializeComponent();
        namedControlsLoaded = true;
        UpdateVisualState();
    }

    /// <summary>
    /// Raised when a command icon or menu item is invoked.
    /// </summary>
    public event EventHandler<DockPaneCommandEventArgs>? CommandRequested;

    /// <summary>
    /// Raised when a docked panel header is dragged past the threshold and released.
    /// </summary>
    public event EventHandler<DockPaneDragCompletedEventArgs>? HeaderDragCompleted;

    /// <summary>
    /// Raised when a floating window should start a native window move drag.
    /// </summary>
    public event EventHandler<PointerPressedEventArgs>? MoveDragRequested;

    /// <summary>
    /// Gets or sets the stable panel identifier.
    /// </summary>
    public string PanelId
    {
        get => GetValue(PanelIdProperty);
        set => SetValue(PanelIdProperty, value);
    }

    /// <summary>
    /// Gets or sets the panel title.
    /// </summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    /// Gets or sets the Segoe MDL2 glyph shown beside the title.
    /// </summary>
    public string Glyph
    {
        get => GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>
    /// Gets or sets the panel body content.
    /// </summary>
    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this panel is hosted by a floating window.
    /// </summary>
    public bool IsFloating
    {
        get => GetValue(IsFloatingProperty);
        set => SetValue(IsFloatingProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the body is collapsed into an auto-hide tab.
    /// </summary>
    public bool IsAutoHidden
    {
        get => GetValue(IsAutoHiddenProperty);
        set => SetValue(IsAutoHiddenProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the float command is enabled.
    /// </summary>
    public bool CanFloat
    {
        get => GetValue(CanFloatProperty);
        set => SetValue(CanFloatProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dock command is enabled.
    /// </summary>
    public bool CanDock
    {
        get => GetValue(CanDockProperty);
        set => SetValue(CanDockProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether auto-hide is enabled.
    /// </summary>
    public bool CanAutoHide
    {
        get => GetValue(CanAutoHideProperty);
        set => SetValue(CanAutoHideProperty, value);
    }

    private void HeaderPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (IsFloating)
        {
            MoveDragRequested?.Invoke(this, e);
            return;
        }

        headerDragStart = e.GetPosition(TopLevel.GetTopLevel(this));
        isHeaderDragging = false;
        e.Pointer.Capture(HeaderBorder);
    }

    private void HeaderPointerMoved(object? sender, PointerEventArgs e)
    {
        if (headerDragStart is not { } start || IsFloating)
        {
            return;
        }

        var current = e.GetPosition(TopLevel.GetTopLevel(this));
        if (Math.Abs(current.X - start.X) > 7 || Math.Abs(current.Y - start.Y) > 7)
        {
            isHeaderDragging = true;
            PseudoClasses.Add(":dragging");
        }
    }

    private void HeaderPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (headerDragStart is null)
        {
            return;
        }

        var current = e.GetPosition(TopLevel.GetTopLevel(this));
        var wasDragging = isHeaderDragging;
        headerDragStart = null;
        isHeaderDragging = false;
        PseudoClasses.Remove(":dragging");
        e.Pointer.Capture(null);

        if (wasDragging)
        {
            HeaderDragCompleted?.Invoke(
                this,
                new DockPaneDragCompletedEventArgs(PanelId, current));
        }
    }

    private void DockClicked(object? sender, RoutedEventArgs e)
    {
        RaiseCommand(DockPaneCommand.Dock);
    }

    private void FloatClicked(object? sender, RoutedEventArgs e)
    {
        RaiseCommand(DockPaneCommand.Float);
    }

    private void AutoHideClicked(object? sender, RoutedEventArgs e)
    {
        IsAutoHidden = !IsAutoHidden;
        RaiseCommand(DockPaneCommand.AutoHide);
    }

    private void CloseClicked(object? sender, RoutedEventArgs e)
    {
        if (!IsFloating)
        {
            IsAutoHidden = true;
        }

        RaiseCommand(DockPaneCommand.Close);
    }

    private void AutoHidePlaceholderPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }

        IsAutoHidden = false;
        RaiseCommand(DockPaneCommand.Restore);
    }

    private void RaiseCommand(DockPaneCommand command)
    {
        CommandRequested?.Invoke(this, new DockPaneCommandEventArgs(PanelId, command));
    }

    private void UpdateVisualState()
    {
        if (!namedControlsLoaded)
        {
            return;
        }

        TitleText.Text = Title;
        AutoHideTitleText.Text = Title;
        GlyphText.Text = Glyph;
        BodyHost.Content = Body;
        BodyHost.IsVisible = !IsAutoHidden;
        AutoHidePlaceholder.IsVisible = IsAutoHidden;
        AutoHideButton.IsEnabled = CanAutoHide;
        FloatButton.IsEnabled = CanFloat && !IsFloating;
        DockButton.IsEnabled = CanDock;
        DockButton.IsVisible = IsFloating;
        RootBorder.Classes.Set("vs-active-dock", !IsFloating);
        HeaderBorder.Classes.Set("vs-active-header", !IsFloating);
        AutoHideButton.Content = IsAutoHidden ? "\uE77A" : "\uE718";
        ToolTip.SetTip(AutoHideButton, IsAutoHidden ? "Pin" : "Auto Hide");
    }
}

/// <summary>
/// Commands emitted by a dock panel header.
/// </summary>
public enum DockPaneCommand
{
    /// <summary>
    /// Dock a floating panel into the main shell.
    /// </summary>
    Dock,

    /// <summary>
    /// Float a docked panel into a generic tool window.
    /// </summary>
    Float,

    /// <summary>
    /// Toggle auto-hide state.
    /// </summary>
    AutoHide,

    /// <summary>
    /// Hide the panel body.
    /// </summary>
    Close,

    /// <summary>
    /// Restore an auto-hidden panel.
    /// </summary>
    Restore,
}

/// <summary>
/// Command event emitted by <see cref="DockPaneChrome"/>.
/// </summary>
/// <param name="PanelId">Panel identifier.</param>
/// <param name="Command">Requested command.</param>
public sealed record DockPaneCommandEventArgs(string PanelId, DockPaneCommand Command);

/// <summary>
/// Header drag completion event emitted by <see cref="DockPaneChrome"/>.
/// </summary>
/// <param name="PanelId">Panel identifier.</param>
/// <param name="PositionInTopLevel">Pointer position in top-level coordinates.</param>
public sealed record DockPaneDragCompletedEventArgs(string PanelId, Point PositionInTopLevel);
