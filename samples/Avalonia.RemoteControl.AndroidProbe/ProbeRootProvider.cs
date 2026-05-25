using Avalonia.Controls;
using Avalonia.RemoteControl.Server;

namespace Avalonia.RemoteControl.AndroidProbe;

/// <summary>
/// Provides the current probe root control to the remote-control runtime.
/// </summary>
public sealed class ProbeRootProvider : IRemoteControlRootProvider
{
    private Control? root;

    /// <summary>
    /// Sets the current probe root control.
    /// </summary>
    /// <param name="control">The root control exposed to the remote-control runtime.</param>
    public void SetRoot(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        root = control;
    }

    /// <inheritdoc />
    public Control? GetRootControl() => root;
}
