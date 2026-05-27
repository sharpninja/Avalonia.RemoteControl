namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Persisted desktop client layout state for a remote-control project.
/// </summary>
public sealed class RemoteControlClientLayoutState
{
    /// <summary>
    /// Gets or sets the main window width in desktop independent pixels.
    /// </summary>
    public double WindowWidth { get; set; } = 1180;

    /// <summary>
    /// Gets or sets the main window height in desktop independent pixels.
    /// </summary>
    public double WindowHeight { get; set; } = 760;

    /// <summary>
    /// Gets or sets the optional main window X position.
    /// </summary>
    public double? WindowX { get; set; }

    /// <summary>
    /// Gets or sets the optional main window Y position.
    /// </summary>
    public double? WindowY { get; set; }

    /// <summary>
    /// Gets or sets the persisted window state name.
    /// </summary>
    public string WindowState { get; set; } = "Normal";

    /// <summary>
    /// Gets or sets the control tree pane width.
    /// </summary>
    public double TreePaneWidth { get; set; } = 340;

    /// <summary>
    /// Gets or sets the right dock pane width.
    /// </summary>
    public double RightPaneWidth { get; set; } = 390;

    /// <summary>
    /// Gets or sets the bottom log pane height.
    /// </summary>
    public double LogPaneHeight { get; set; } = 220;

    /// <summary>
    /// Gets or sets the selected right-side tool tab index.
    /// </summary>
    public int RightToolTabIndex { get; set; }

    /// <summary>
    /// Gets or sets the selected default workspace tab index.
    /// </summary>
    public int WorkspaceTabIndex { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether logs were popped out.
    /// </summary>
    public bool LogsPoppedOut { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether live view was docked in the main window.
    /// </summary>
    public bool LiveViewDocked { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the control tree pane is auto-hidden.
    /// </summary>
    public bool ControlTreeAutoHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the properties pane is auto-hidden.
    /// </summary>
    public bool PropertiesAutoHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the remote tools pane is auto-hidden.
    /// </summary>
    public bool RemoteToolsAutoHidden { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the logs pane is auto-hidden.
    /// </summary>
    public bool LogsAutoHidden { get; set; }
}
