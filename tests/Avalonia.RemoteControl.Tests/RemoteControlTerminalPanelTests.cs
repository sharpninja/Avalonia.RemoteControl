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
        Assert.Equal(Environment.CurrentDirectory, viewModel.StartupWorkingDirectory);
        Assert.Equal(Environment.CurrentDirectory, viewModel.EffectiveWorkingDirectory);
        Assert.False(viewModel.IsRunning);
        Assert.Null(viewModel.ProcessId);
        Assert.Null(viewModel.ExitCode);
    }

    [Fact]
    public void TerminalPanelViewModelUsesCapturedStartupWorkingDirectory()
    {
        var startupWorkingDirectory = Path.Combine(Path.GetTempPath(), "arc-startup-cwd");
        var laterWorkingDirectory = Path.Combine(Path.GetTempPath(), "arc-later-cwd");
        var viewModel = new TerminalPanelViewModel(startupWorkingDirectory)
        {
            RemoteControlMcpUrl = "http://127.0.0.1:49111/mcp/test",
            WorkingDirectory = string.Empty,
        };
        var previousCurrentDirectory = Environment.CurrentDirectory;
        Directory.CreateDirectory(laterWorkingDirectory);

        try
        {
            Environment.CurrentDirectory = laterWorkingDirectory;

            viewModel.ApplyCodexMcpPreset();

            Assert.Equal(Path.GetFullPath(startupWorkingDirectory), viewModel.StartupWorkingDirectory);
            Assert.Equal(Path.GetFullPath(startupWorkingDirectory), viewModel.WorkingDirectory);
            Assert.Equal(Path.GetFullPath(startupWorkingDirectory), viewModel.EffectiveWorkingDirectory);
        }
        finally
        {
            Environment.CurrentDirectory = previousCurrentDirectory;
        }
    }

    [Fact]
    public void TerminalPanelViewModelAppliesCodexPreset()
    {
        var viewModel = new TerminalPanelViewModel
        {
            RemoteControlMcpUrl = "http://127.0.0.1:49111/mcp/test",
            WorkingDirectory = string.Empty,
        };

        viewModel.ApplyCodexPreset();

        Assert.False(string.IsNullOrWhiteSpace(viewModel.Command));
        Assert.Contains("-NoProfile", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("codex", viewModel.Arguments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mcp_servers.avalonia_remote_control", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("url", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("http://127.0.0.1:49111/mcp/test", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.GetCapabilities, viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.GetSnapshot, viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.InvokeClick, viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("Do not use screenshots", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("avalonia-remote", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("args=[", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Equal(Environment.CurrentDirectory, viewModel.WorkingDirectory);
    }

    [Fact]
    public void TerminalPanelViewModelAppliesCodexMcpPresetWithoutCommandLineToken()
    {
        var viewModel = new TerminalPanelViewModel
        {
            RemoteControlEndpoint = "http://127.0.0.1:47100/",
            RemoteControlToken = "secret-token",
            RemoteControlTransportProtocol = "arc-protobuf-v1",
            RemoteControlMcpUrl = "http://127.0.0.1:49111/mcp/test",
            WorkingDirectory = string.Empty,
        };

        viewModel.ApplyCodexMcpPreset();

        Assert.Contains("mcp_servers.avalonia_remote_control", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("url", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("http://127.0.0.1:49111/mcp/test", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.GetSnapshot, viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains(RemoteControlMcpToolCatalog.SetProperty, viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("control tree", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("avalonia-remote", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("stdio", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("arc-protobuf-v1", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("AVALONIA_REMOTE_CONTROL_TOKEN", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--profile", viewModel.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-token", viewModel.Arguments, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceViewModelHostsTerminalInDefaultWorkspaceState()
    {
        var startupWorkingDirectory = Path.Combine(Path.GetTempPath(), "arc-workspace-cwd");
        var viewModel = new WorkspacePanelViewModel(startupWorkingDirectory);

        Assert.NotNull(viewModel.Terminal);
        Assert.NotNull(viewModel.Properties);
        Assert.Equal(Path.GetFullPath(startupWorkingDirectory), viewModel.Terminal.WorkingDirectory);
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

    [Fact]
    public void DockLayoutReservesOnlyStripForAutoHiddenDockRegions()
    {
        var layout = new DockLayout
        {
            WestWidth = 100,
            EastWidth = 100,
            SouthHeight = 50,
            AutoHideStripThickness = 34,
            DockSpacing = 10,
        };
        var west = new AutoHiddenDockHost();
        var east = new AutoHiddenDockHost();
        var south = new AutoHiddenDockHost();
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

        Assert.Equal(new Rect(0, 0, 34, 300), west.Bounds);
        Assert.Equal(new Rect(466, 0, 34, 300), east.Bounds);
        Assert.Equal(new Rect(44, 266, 412, 34), south.Bounds);
        Assert.Equal(new Rect(44, 0, 412, 256), fill.Bounds);
    }

    private sealed class AutoHiddenDockHost : Border, IDockAutoHideHost
    {
        public bool IsAutoHidden => true;
    }
}
