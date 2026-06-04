using Avalonia.RemoteControl.Client.Live;
using Avalonia.RemoteControl.Client.Logging;
using Avalonia.RemoteControl.Client.Projects;
using Avalonia.RemoteControl.Protocol.V1;

namespace Avalonia.RemoteControl.Tool;

/// <summary>
/// Testable view-model root for the desktop tool shell.
/// </summary>
public sealed class RemoteControlToolShellViewModel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlToolShellViewModel"/> class.
    /// </summary>
    public RemoteControlToolShellViewModel()
        : this(ToolProcessContext.StartupWorkingDirectory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlToolShellViewModel"/> class.
    /// </summary>
    /// <param name="startupWorkingDirectory">Startup working directory for the embedded terminal.</param>
    public RemoteControlToolShellViewModel(string startupWorkingDirectory)
    {
        Workspace = new WorkspacePanelViewModel(startupWorkingDirectory);
    }

    /// <summary>
    /// Gets the control-tree panel state.
    /// </summary>
    public ControlTreePanelViewModel ControlTree { get; } = new();

    /// <summary>
    /// Gets the workspace panel state.
    /// </summary>
    public WorkspacePanelViewModel Workspace { get; }

    /// <summary>
    /// Gets the remote tools panel state.
    /// </summary>
    public RemoteToolsPanelViewModel RemoteTools { get; } = new();

    /// <summary>
    /// Gets the shared log panel state.
    /// </summary>
    public RemoteLogViewModel Logs { get; } = new();

    /// <summary>
    /// Gets or sets live-view capabilities for the current connection.
    /// </summary>
    public RemoteLiveViewCapabilities LiveViewCapabilities { get; set; } = RemoteLiveViewCapabilities.None;

    /// <summary>
    /// Gets or sets the audit identity reported by the connected endpoint.
    /// </summary>
    public string AuthenticatedClientIdentity { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the live view should be docked after connecting.
    /// </summary>
    public bool RestoreDockedLiveViewOnConnect { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether the shell starts without an active live-view control.
    /// </summary>
    public bool StartsWithoutLiveViewContent => !RemoteTools.LiveView.HasContent;

    /// <summary>
    /// Gets a value indicating whether frame streaming is disabled in the shell startup state.
    /// </summary>
    public bool StartsWithFrameStreamingDisabled => !LiveViewCapabilities.SupportsFrameStreaming;

    /// <summary>
    /// Resets connection-scoped live-view state after disconnect or connection failure.
    /// </summary>
    public void ResetConnectionState()
    {
        LiveViewCapabilities = RemoteLiveViewCapabilities.None;
        AuthenticatedClientIdentity = string.Empty;
        RemoteTools.LiveView.Content = null;
    }

    /// <summary>
    /// Applies capabilities after a successful remote connection.
    /// </summary>
    /// <param name="capabilities">Protocol capabilities.</param>
    public void ApplyCapabilities(GetCapabilitiesResponse capabilities)
    {
        LiveViewCapabilities = RemoteLiveViewCapabilities.FromProtocol(capabilities);
        AuthenticatedClientIdentity = capabilities.AuthenticatedClientIdentity;
    }

    /// <summary>
    /// Applies persisted shell layout defaults that are not tied to Avalonia controls.
    /// </summary>
    /// <param name="layout">Persisted layout state.</param>
    public void ApplyLayoutState(RemoteControlClientLayoutState? layout)
    {
        if (layout is null)
        {
            RestoreDockedLiveViewOnConnect = true;
            return;
        }

        RestoreDockedLiveViewOnConnect = layout.LiveViewDockStateInitialized
            ? layout.LiveViewDocked
            : true;
    }
}
