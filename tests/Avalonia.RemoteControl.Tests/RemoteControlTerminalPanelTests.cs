using Avalonia.RemoteControl.Tool;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.RemoteControl.Client.Projects;
using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlTerminalPanelTests
{
    [Fact]
    public void TerminalPanelViewModelDefaultsToInteractiveShell()
    {
        var viewModel = new TerminalPanelViewModel();

        Assert.False(string.IsNullOrWhiteSpace(viewModel.Command));
        Assert.Contains("-NoProfile", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("-NonInteractive", viewModel.Arguments, StringComparison.Ordinal);
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
    public void TerminalPanelViewModelResolvesStaleWorkspaceFolderToGitCheckout()
    {
        var root = Path.Combine(Path.GetTempPath(), "arc-workspace-roots", Guid.NewGuid().ToString("N"));
        var workspaceRoot = Path.Combine(root, "github");
        var staleWorkspace = Path.Combine(root, "stale", "Avalonia.RemoteControl");
        var realWorkspace = Path.Combine(workspaceRoot, "Avalonia.RemoteControl");
        Directory.CreateDirectory(staleWorkspace);
        Directory.CreateDirectory(Path.Combine(realWorkspace, ".git"));
        var previousRoots = Environment.GetEnvironmentVariable(ToolProcessContext.WorkspaceRootsEnvironmentVariable);

        try
        {
            Environment.SetEnvironmentVariable(ToolProcessContext.WorkspaceRootsEnvironmentVariable, workspaceRoot);

            var viewModel = new TerminalPanelViewModel(staleWorkspace);

            Assert.Equal(Path.GetFullPath(realWorkspace), viewModel.StartupWorkingDirectory);
            Assert.Equal(Path.GetFullPath(realWorkspace), viewModel.WorkingDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(ToolProcessContext.WorkspaceRootsEnvironmentVariable, previousRoots);
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
        Assert.Contains("-NonInteractive", viewModel.Arguments, StringComparison.Ordinal);
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
    public void TerminalPanelViewModelAppliesNonInteractiveShellPreset()
    {
        var viewModel = new TerminalPanelViewModel
        {
            Arguments = "-Command codex",
            WorkingDirectory = string.Empty,
        };

        viewModel.ApplyShellPreset();

        Assert.Contains("-NoProfile", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Contains("-NonInteractive", viewModel.Arguments, StringComparison.Ordinal);
        Assert.Equal(Environment.CurrentDirectory, viewModel.WorkingDirectory);
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
    public void ToolShellDefaultsLiveViewDockedWithFramesDisabledAndNoContent()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());

        Assert.True(shell.RestoreDockedLiveViewOnConnect);
        Assert.True(shell.StartsWithFrameStreamingDisabled);
        Assert.True(shell.StartsWithoutLiveViewContent);
        Assert.False(shell.LiveViewCapabilities.SupportsFrameStreaming);
        Assert.False(shell.LiveViewCapabilities.SupportsRemoteInput);
        Assert.Equal(string.Empty, shell.AuthenticatedClientIdentity);
        Assert.Null(shell.RemoteTools.LiveView.Content);
    }

    [Fact]
    public void ToolShellAppliesPersistedLiveViewDockState()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());

        shell.ApplyLayoutState(new RemoteControlClientLayoutState
        {
            LiveViewDocked = false,
            LiveViewDockStateInitialized = true,
        });

        Assert.False(shell.RestoreDockedLiveViewOnConnect);

        shell.ApplyLayoutState(new RemoteControlClientLayoutState
        {
            LiveViewDocked = false,
            LiveViewDockStateInitialized = false,
        });

        Assert.True(shell.RestoreDockedLiveViewOnConnect);
    }

    [Fact]
    public void ToolShellResetsConnectionScopedLiveViewState()
    {
        var shell = new RemoteControlToolShellViewModel(Path.GetTempPath());
        shell.ApplyCapabilities(new GetCapabilitiesResponse
        {
            AuthenticatedClientIdentity = "desktop-client",
            SupportsFrameStreaming = true,
            SupportsRemoteInput = true,
        });
        shell.RemoteTools.LiveView.Content = new object();

        Assert.Equal("desktop-client", shell.AuthenticatedClientIdentity);

        shell.ResetConnectionState();

        Assert.True(shell.StartsWithFrameStreamingDisabled);
        Assert.True(shell.StartsWithoutLiveViewContent);
        Assert.False(shell.LiveViewCapabilities.SupportsRemoteInput);
        Assert.Equal(string.Empty, shell.AuthenticatedClientIdentity);
    }

    [AvaloniaFact]
    public void ControlTreePanelSelectItemRevealsNestedLiveViewSelection()
    {
        var viewModel = new ControlTreePanelViewModel();
        var root = new RemoteTreeItem(CreateTreeNode("root", "Root"));
        var panelItem = new RemoteTreeItem(CreateTreeNode("panel", "Panel", "root"));
        var button = new RemoteTreeItem(CreateTreeNode("button", "Button", "panel"));
        root.AddChild(panelItem);
        panelItem.AddChild(button);
        viewModel.Items.Add(root);

        var control = new ControlTreePanel
        {
            ViewModel = viewModel,
        };

        control.SelectItem(button);

        Assert.Same(button, viewModel.SelectedItem);
        Assert.True(root.IsExpanded);
        Assert.True(panelItem.IsExpanded);
        Assert.False(button.IsExpanded);
        Assert.Same(root, panelItem.Parent);
        Assert.Same(panelItem, button.Parent);
    }

    [Fact]
    public void McpHostControllerStartsRestartsAndDisposesEndpoint()
    {
        var terminal = new TerminalPanelViewModel(Path.GetTempPath());
        var hosts = new List<RecordingMcpEndpointHost>();
        var capturedFactories = new List<Func<RemoteControlMcpOptions>>();
        var endpointIndex = 0;
        var token = "first-token";
        var controller = new RemoteControlMcpHostController(
            terminal,
            () => RemoteControlMcpOptions.Create(new Uri("http://127.0.0.1:47100/"), token),
            optionsFactory =>
            {
                capturedFactories.Add(optionsFactory);
                var host = new RecordingMcpEndpointHost(new Uri($"http://127.0.0.1:49{endpointIndex++}/mcp/test"));
                hosts.Add(host);
                return host;
            });

        controller.Start();
        controller.Start();

        Assert.True(controller.IsRunning);
        Assert.Single(hosts);
        Assert.Equal(hosts[0].Endpoint.ToString(), terminal.RemoteControlMcpUrl);
        token = "second-token";
        Assert.Equal("second-token", capturedFactories[0]().Token);

        controller.Restart();

        Assert.True(hosts[0].Disposed);
        Assert.Equal(2, hosts.Count);
        Assert.Equal(hosts[1].Endpoint.ToString(), terminal.RemoteControlMcpUrl);

        controller.Dispose();

        Assert.True(hosts[1].Disposed);
        Assert.False(controller.IsRunning);
        Assert.Equal(string.Empty, terminal.RemoteControlMcpUrl);
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
    public void DockLayoutMeasuresNestedSideDockWithFiniteSize()
    {
        var outer = new DockLayout
        {
            EastWidth = 120,
            SouthHeight = 50,
            DockSpacing = 10,
        };
        var inner = new DockLayout
        {
            SouthHeight = 60,
            DockSpacing = 5,
        };
        DockLayout.SetRegion(inner, DockRegion.East);
        DockLayout.SetRegion(new Border(), DockRegion.Fill);
        inner.Children.Add(new Border());
        var innerSouth = new Border();
        DockLayout.SetRegion(innerSouth, DockRegion.South);
        inner.Children.Add(innerSouth);
        outer.Children.Add(inner);
        outer.Children.Add(new Border());

        outer.Measure(new Size(500, 300));
        outer.Arrange(new Rect(0, 0, 500, 300));

        Assert.Equal(new Size(500, 300), outer.DesiredSize);
        Assert.Equal(new Rect(380, 0, 120, 300), inner.Bounds);
    }

    [Fact]
    public void DockLayoutCoercesInfiniteMeasureToFiniteDesiredSize()
    {
        var layout = new DockLayout();
        layout.Children.Add(new Border());

        layout.Measure(new Size(390, double.PositiveInfinity));

        Assert.True(double.IsFinite(layout.DesiredSize.Width));
        Assert.True(double.IsFinite(layout.DesiredSize.Height));
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

    private static TreeNode CreateTreeNode(string id, string typeName, string parentId = "")
    {
        return new TreeNode
        {
            Id = id,
            TypeName = typeName,
            ParentId = parentId,
            IsVisible = true,
            IsEnabled = true,
            AbsoluteBounds = new Avalonia.RemoteControl.Protocol.V1.Rect
            {
                Width = 100,
                Height = 20,
            },
        };
    }

    private sealed class RecordingMcpEndpointHost : IRemoteControlMcpEndpointHost
    {
        public RecordingMcpEndpointHost(Uri endpoint)
        {
            Endpoint = endpoint;
        }

        public Uri Endpoint { get; }

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
