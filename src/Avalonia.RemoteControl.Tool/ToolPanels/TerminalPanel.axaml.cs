using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Iciclecreek.Terminal;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Embedded terminal tool panel.
/// </summary>
public sealed partial class TerminalPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TerminalPanel"/> class.
    /// </summary>
    public TerminalPanel()
    {
        InitializeComponent();
        ViewModel = new TerminalPanelViewModel();
    }

    /// <summary>
    /// Gets or sets the terminal panel view model.
    /// </summary>
    public TerminalPanelViewModel? ViewModel
    {
        get => DataContext as TerminalPanelViewModel;
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

            UpdatePresentationState();
        }
    }

    private void LaunchCodexClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyCodexPreset();
        LaunchConfiguredProcess();
    }

    private void LaunchShellClicked(object? sender, RoutedEventArgs e)
    {
        ViewModel?.ApplyShellPreset();
        LaunchConfiguredProcess();
    }

    private void StopClicked(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        if (!viewModel.IsRunning)
        {
            viewModel.StatusText = "Terminal is not running.";
            return;
        }

        try
        {
            Terminal.Kill();
            viewModel.StatusText = "Terminal stop requested.";
        }
        catch (Exception ex)
        {
            viewModel.StatusText = $"Stop failed: {ex.Message}";
        }
    }

    private void LaunchConfiguredProcess()
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        if (viewModel.IsRunning)
        {
            viewModel.StatusText = "Terminal process is already running.";
            return;
        }

        var process = viewModel.Command.Trim();
        if (string.IsNullOrWhiteSpace(process))
        {
            viewModel.StatusText = "Enter a terminal command.";
            return;
        }

        try
        {
            var workingDirectory = viewModel.EffectiveWorkingDirectory;
            var args = TerminalCommandLine.ParseArguments(viewModel.Arguments);
            Terminal.LaunchProcess(workingDirectory, process, args);

            viewModel.IsRunning = true;
            viewModel.ExitCode = null;
            viewModel.ProcessId = Terminal.Pid > 0 ? Terminal.Pid : null;
            viewModel.StatusText = viewModel.ProcessId is { } pid
                ? $"Terminal running: pid {pid}."
                : "Terminal running.";
        }
        catch (Exception ex)
        {
            viewModel.IsRunning = false;
            viewModel.ProcessId = null;
            viewModel.StatusText = $"Launch failed: {ex.Message}";
        }
    }

    private void TerminalProcessExited(object? sender, ProcessExitedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ViewModel is not { } viewModel)
            {
                return;
            }

            viewModel.IsRunning = false;
            viewModel.ProcessId = null;
            viewModel.ExitCode = e.ExitCode;
            viewModel.StatusText = $"Terminal exited with code {e.ExitCode}.";
        });
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.Equals(e.PropertyName, nameof(TerminalPanelViewModel.IsRunning), StringComparison.Ordinal))
        {
            UpdatePresentationState();
        }
    }

    private void UpdatePresentationState()
    {
        var running = ViewModel?.IsRunning == true;
        LaunchCodexButton.IsEnabled = !running;
        LaunchShellButton.IsEnabled = !running;
        StopButton.IsEnabled = running;
    }
}
