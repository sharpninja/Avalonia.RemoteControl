using Android.App;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;

namespace Avalonia.RemoteControl.AndroidProbe.Android;

/// <summary>
/// Android activity hosting the Avalonia remote-control probe app.
/// </summary>
[Activity(
    Label = "Avalonia RemoteControl Probe",
    Theme = "@style/Theme.App.Splash",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.UiMode
        | ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : AvaloniaMainActivity
{
    private AndroidProbeBridgeHost? bridgeHost;

    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        bridgeHost = AndroidProbeBridgeHost.Start(this, App.RootProvider);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        bridgeHost?.Dispose();
        bridgeHost = null;
        base.OnDestroy();
    }
}
