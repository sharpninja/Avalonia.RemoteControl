using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Avalonia.RemoteControl.Client.Logging;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Base class for desktop tool-panel view models.
/// </summary>
public abstract class ToolPanelViewModel : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises a property changed event.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a backing field and raises <see cref="PropertyChanged"/> when the value changes.
    /// </summary>
    /// <typeparam name="T">Value type.</typeparam>
    /// <param name="field">Backing field.</param>
    /// <param name="value">New value.</param>
    /// <param name="propertyName">Property name.</param>
    /// <returns><see langword="true"/> when the value changed.</returns>
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

/// <summary>
/// View model for the remote control tree panel.
/// </summary>
public sealed class ControlTreePanelViewModel : ToolPanelViewModel
{
    private RemoteTreeItem? selectedItem;

    /// <summary>
    /// Gets the tree roots.
    /// </summary>
    public ObservableCollection<RemoteTreeItem> Items { get; } = [];

    /// <summary>
    /// Gets or sets the selected tree item.
    /// </summary>
    public RemoteTreeItem? SelectedItem
    {
        get => selectedItem;
        set => SetField(ref selectedItem, value);
    }
}

/// <summary>
/// View model for the selected-node property panel.
/// </summary>
public sealed class PropertiesPanelViewModel : ToolPanelViewModel
{
    private PropertyRow? selectedItem;

    /// <summary>
    /// Gets property rows for the selected node.
    /// </summary>
    public ObservableCollection<PropertyRow> Rows { get; } = [];

    /// <summary>
    /// Gets or sets the selected property row.
    /// </summary>
    public PropertyRow? SelectedItem
    {
        get => selectedItem;
        set => SetField(ref selectedItem, value);
    }
}

/// <summary>
/// View model for the actions panel.
/// </summary>
public sealed class ActionsPanelViewModel : ToolPanelViewModel
{
    private string propertyName = string.Empty;
    private string propertyValue = string.Empty;

    /// <summary>
    /// Gets or sets the property name to edit.
    /// </summary>
    public string PropertyName
    {
        get => propertyName;
        set => SetField(ref propertyName, value);
    }

    /// <summary>
    /// Gets or sets the property value to send.
    /// </summary>
    public string PropertyValue
    {
        get => propertyValue;
        set => SetField(ref propertyValue, value);
    }
}

/// <summary>
/// View model for the project panel.
/// </summary>
public sealed class ProjectPanelViewModel : ToolPanelViewModel
{
    private string summaryText = "Project not loaded.";
    private string sessionText = "No active session.";
    private string replayText = "Replay data will appear after interactions are recorded.";

    /// <summary>
    /// Gets or sets the project summary text.
    /// </summary>
    public string SummaryText
    {
        get => summaryText;
        set => SetField(ref summaryText, value);
    }

    /// <summary>
    /// Gets or sets the active session text.
    /// </summary>
    public string SessionText
    {
        get => sessionText;
        set => SetField(ref sessionText, value);
    }

    /// <summary>
    /// Gets or sets the replay summary text.
    /// </summary>
    public string ReplayText
    {
        get => replayText;
        set => SetField(ref replayText, value);
    }
}

/// <summary>
/// View model for the live-view panel host.
/// </summary>
public sealed class LiveViewPanelViewModel : ToolPanelViewModel
{
    private object? content;
    private string placeholderText = "Connect and dock live view to start streaming.";

    /// <summary>
    /// Gets or sets the active live-view control.
    /// </summary>
    public object? Content
    {
        get => content;
        set
        {
            if (SetField(ref content, value))
            {
                OnPropertyChanged(nameof(HasContent));
            }
        }
    }

    /// <summary>
    /// Gets or sets placeholder text shown when no live view is active.
    /// </summary>
    public string PlaceholderText
    {
        get => placeholderText;
        set => SetField(ref placeholderText, value);
    }

    /// <summary>
    /// Gets a value indicating whether an active live-view control exists.
    /// </summary>
    public bool HasContent => Content is not null;
}

/// <summary>
/// View model for the default fill workspace panel.
/// </summary>
public sealed class WorkspacePanelViewModel : ToolPanelViewModel
{
    private int selectedTabIndex;

    /// <summary>
    /// Gets the embedded terminal panel view model.
    /// </summary>
    public TerminalPanelViewModel Terminal { get; } = new();

    /// <summary>
    /// Gets the selected-node property panel view model.
    /// </summary>
    public PropertiesPanelViewModel Properties { get; } = new();

    /// <summary>
    /// Gets or sets the selected workspace tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => SetField(ref selectedTabIndex, value);
    }
}

/// <summary>
/// View model for the right-side remote tools tab surface.
/// </summary>
public sealed class RemoteToolsPanelViewModel : ToolPanelViewModel
{
    private int selectedTabIndex;

    /// <summary>
    /// Gets the action panel view model.
    /// </summary>
    public ActionsPanelViewModel Actions { get; } = new();

    /// <summary>
    /// Gets the live-view panel view model.
    /// </summary>
    public LiveViewPanelViewModel LiveView { get; } = new();

    /// <summary>
    /// Gets the project panel view model.
    /// </summary>
    public ProjectPanelViewModel Project { get; } = new();

    /// <summary>
    /// Gets or sets the selected remote-tools tab index.
    /// </summary>
    public int SelectedTabIndex
    {
        get => selectedTabIndex;
        set => SetField(ref selectedTabIndex, value);
    }
}

/// <summary>
/// View model for the embedded terminal panel.
/// </summary>
public sealed class TerminalPanelViewModel : ToolPanelViewModel
{
    private string command = GetDefaultShellProcess();
    private string arguments = "-NoLogo -NoProfile";
    private string workingDirectory = Environment.CurrentDirectory;
    private string statusText = "Terminal stopped.";
    private bool isRunning;
    private int? processId;
    private int? exitCode;

    /// <summary>
    /// Gets or sets the executable or shell command to launch.
    /// </summary>
    public string Command
    {
        get => command;
        set => SetField(ref command, value);
    }

    /// <summary>
    /// Gets or sets the command-line arguments to pass to <see cref="Command"/>.
    /// </summary>
    public string Arguments
    {
        get => arguments;
        set => SetField(ref arguments, value);
    }

    /// <summary>
    /// Gets or sets the process working directory.
    /// </summary>
    public string WorkingDirectory
    {
        get => workingDirectory;
        set => SetField(ref workingDirectory, value);
    }

    /// <summary>
    /// Gets or sets the terminal status text.
    /// </summary>
    public string StatusText
    {
        get => statusText;
        set => SetField(ref statusText, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether a process is running.
    /// </summary>
    public bool IsRunning
    {
        get => isRunning;
        set => SetField(ref isRunning, value);
    }

    /// <summary>
    /// Gets or sets the launched process identifier.
    /// </summary>
    public int? ProcessId
    {
        get => processId;
        set => SetField(ref processId, value);
    }

    /// <summary>
    /// Gets or sets the latest process exit code.
    /// </summary>
    public int? ExitCode
    {
        get => exitCode;
        set => SetField(ref exitCode, value);
    }

    /// <summary>
    /// Applies the default Codex launch command.
    /// </summary>
    public void ApplyCodexPreset()
    {
        Command = GetDefaultShellProcess();
        Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "-NoLogo -NoProfile -Command codex"
            : "-NoLogo -NoProfile -Command codex";
        if (string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            WorkingDirectory = Environment.CurrentDirectory;
        }
    }

    /// <summary>
    /// Applies an interactive shell preset.
    /// </summary>
    public void ApplyShellPreset()
    {
        Command = GetDefaultShellProcess();
        Arguments = "-NoLogo -NoProfile";
        if (string.IsNullOrWhiteSpace(WorkingDirectory))
        {
            WorkingDirectory = Environment.CurrentDirectory;
        }
    }

    private static string GetDefaultShellProcess()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "pwsh.exe" : "pwsh";
    }
}
