using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Avalonia.RemoteControl.Client.Logging;

/// <summary>
/// Shared client-side log view state used by embedded and pop-out log views.
/// </summary>
public sealed class RemoteLogViewModel : INotifyPropertyChanged
{
    private string statusText = "Log stream stopped.";
    private bool isPoppedOut;
    private RemoteLogVerbosity selectedVerbosity = RemoteLogVerbosity.Default;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets the shared display rows for remote logs and client log-stream status messages.
    /// </summary>
    public ObservableCollection<string> Rows { get; } = [];

    /// <summary>
    /// Gets the supported remote log verbosity options.
    /// </summary>
    public IReadOnlyList<RemoteLogVerbosity> SupportedVerbosity =>
        RemoteLogVerbosity.Supported;

    /// <summary>
    /// Gets or sets the selected remote log verbosity.
    /// </summary>
    public RemoteLogVerbosity SelectedVerbosity
    {
        get => selectedVerbosity;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (Equals(selectedVerbosity, value))
            {
                return;
            }

            selectedVerbosity = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the current log stream status text.
    /// </summary>
    public string StatusText
    {
        get => statusText;
        private set
        {
            if (string.Equals(statusText, value, StringComparison.Ordinal))
            {
                return;
            }

            statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets a value indicating whether the shared log rows are currently owned by the pop-out window.
    /// </summary>
    public bool IsPoppedOut
    {
        get => isPoppedOut;
        private set
        {
            if (isPoppedOut == value)
            {
                return;
            }

            isPoppedOut = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the number of remote log entries, excluding local client status rows.
    /// </summary>
    public int RemoteEntryCount =>
        Rows.Count(static row => !IsClientStatusRow(row));

    /// <summary>
    /// Updates the current log stream status text.
    /// </summary>
    /// <param name="value">Status text.</param>
    public void SetStatus(string value)
    {
        StatusText = value;
    }

    /// <summary>
    /// Marks the log rows as displayed in the pop-out window.
    /// </summary>
    public void PopOut()
    {
        IsPoppedOut = true;
    }

    /// <summary>
    /// Marks the log rows as displayed in the main window.
    /// </summary>
    public void Dock()
    {
        IsPoppedOut = false;
    }

    private static bool IsClientStatusRow(string row)
    {
        return row.StartsWith("client ", StringComparison.Ordinal);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
