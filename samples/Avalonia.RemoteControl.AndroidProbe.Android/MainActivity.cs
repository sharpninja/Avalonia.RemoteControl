using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Util;
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
    private const string LogTag = "AvaloniaRemoteProbe";

    private readonly object bridgeHostSync = new();
    private AndroidProbeBridgeHost? bridgeHost;
    private CancellationTokenSource? bridgeStartCancellation;

    /// <inheritdoc />
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        bridgeStartCancellation = new CancellationTokenSource();
        _ = StartBridgeHostAsync(bridgeStartCancellation.Token);
    }

    /// <inheritdoc />
    protected override void OnDestroy()
    {
        bridgeStartCancellation?.Cancel();

        AndroidProbeBridgeHost? host;
        lock (bridgeHostSync)
        {
            host = bridgeHost;
            bridgeHost = null;
        }

        host?.Dispose();
        bridgeStartCancellation?.Dispose();
        bridgeStartCancellation = null;
        base.OnDestroy();
    }

    private async Task StartBridgeHostAsync(CancellationToken cancellationToken)
    {
        try
        {
            Log.Info(LogTag, "Starting Avalonia.RemoteControl Android bridge host.");
            var host = await AndroidProbeBridgeHost.StartAsync(
                    this,
                    App.RootProvider,
                    cancellationToken)
                .ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                host.Dispose();
                return;
            }

            lock (bridgeHostSync)
            {
                bridgeHost?.Dispose();
                bridgeHost = host;
            }

            Log.Info(LogTag, "Avalonia.RemoteControl Android bridge host started.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.Info(LogTag, "Avalonia.RemoteControl Android bridge host startup canceled.");
        }
        catch (Exception exception)
        {
            Log.Error(LogTag, exception.ToString());
        }
    }
}
