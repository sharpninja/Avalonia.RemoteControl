using Avalonia.RemoteControl.Tool;
using Avalonia;
using Avalonia.Controls;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlTerminalPanelTests
{
    [Fact]
    public void TerminalPanelViewModelDefaultsToInteractiveShell()
    {
        var viewModel = new TerminalPanelViewModel();

        Assert.False(string.IsNullOrWhiteSpace(viewModel.Command));
        Assert.Contains("-NoProfile", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Equal(Environment.CurrentDirectory, viewModel.WorkingDirectory);
        Assert.False(viewModel.IsRunning);
        Assert.Null(viewModel.ProcessId);
        Assert.Null(viewModel.ExitCode);
    }

    [Fact]
    public void TerminalPanelViewModelAppliesCodexPreset()
    {
        var viewModel = new TerminalPanelViewModel
        {
            WorkingDirectory = string.Empty,
        };

        viewModel.ApplyCodexPreset();

        Assert.False(string.IsNullOrWhiteSpace(viewModel.Command));
        Assert.Contains("-NoProfile", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("codex", viewModel.Arguments, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Environment.CurrentDirectory, viewModel.WorkingDirectory);
    }

    [Fact]
    public void WorkspaceViewModelHostsTerminalInDefaultWorkspaceState()
    {
        var viewModel = new WorkspacePanelViewModel();

        Assert.NotNull(viewModel.Terminal);
        Assert.NotNull(viewModel.Properties);
        viewModel.SelectedTabIndex = 1;
        Assert.Equal(1, viewModel.SelectedTabIndex);
    }

    [Fact]
    public void DockLayoutDefaultsUndeclaredChildrenToFill()
    {
        var child = new Border();

        Assert.Equal(DockRegion.Fill, DockLayout.GetRegion(child));
        DockLayout.SetRegion(child, DockRegion.West);
        Assert.Equal(DockRegion.West, DockLayout.GetRegion(child));
    }

    [Fact]
    public void DockLayoutDoesNotReserveSpaceForHiddenDockRegions()
    {
        var layout = new DockLayout
        {
            WestWidth = 100,
            EastWidth = 100,
            SouthHeight = 50,
            DockSpacing = 10,
        };
        var west = new Border { IsVisible = false };
        var east = new Border { IsVisible = false };
        var south = new Border { IsVisible = false };
        var fill = new Border();
        DockLayout.SetRegion(west, DockRegion.West);
        DockLayout.SetRegion(east, DockRegion.East);
        DockLayout.SetRegion(south, DockRegion.South);

        layout.Children.Add(west);
        layout.Children.Add(east);
        layout.Children.Add(south);
        layout.Children.Add(fill);

        layout.Measure(new Size(500, 300));
        layout.Arrange(new Rect(0, 0, 500, 300));

        Assert.Equal(new Rect(0, 0, 500, 300), fill.Bounds);
    }

    [Fact]
    public void DockLayoutRestoresSpaceForVisibleDockRegions()
    {
        var layout = new DockLayout
        {
            WestWidth = 100,
            EastWidth = 100,
            SouthHeight = 50,
            DockSpacing = 10,
        };
        var west = new Border();
        var east = new Border();
        var south = new Border();
        var fill = new Border();
        DockLayout.SetRegion(west, DockRegion.West);
        DockLayout.SetRegion(east, DockRegion.East);
        DockLayout.SetRegion(south, DockRegion.South);

        layout.Children.Add(west);
        layout.Children.Add(east);
        layout.Children.Add(south);
        layout.Children.Add(fill);

        layout.Measure(new Size(500, 300));
        layout.Arrange(new Rect(0, 0, 500, 300));

        Assert.Equal(new Rect(110, 0, 280, 240), fill.Bounds);
        Assert.Equal(new Rect(110, 250, 280, 50), south.Bounds);
    }
}
