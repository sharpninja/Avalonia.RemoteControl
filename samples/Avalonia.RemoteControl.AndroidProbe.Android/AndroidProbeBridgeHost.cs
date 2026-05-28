using System.Net;
using System.Security.Cryptography;
using Android.Content;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Bridge;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.AndroidProbe.Android;

/// <summary>
/// Starts the Android loopback bridge listener and publishes its package-private marker.
/// </summary>
public sealed class AndroidProbeBridgeHost : IDisposable
{
    /// <summary>
    /// Gets the fixed device-side bridge port used by the probe sample.
    /// </summary>
    public const int DevicePort = 47100;

    private readonly ServiceProvider serviceProvider;
    private readonly RemoteControlBridgeTcpListener listener;

    private AndroidProbeBridgeHost(
        ServiceProvider serviceProvider,
        RemoteControlBridgeTcpListener listener)
    {
        this.serviceProvider = serviceProvider;
        this.listener = listener;
    }

    /// <summary>
    /// Starts the bridge host for the supplied Android context.
    /// </summary>
    /// <param name="context">Android package context used to publish the marker file.</param>
    /// <param name="rootProvider">Avalonia root provider exposed to remote-control runtime services.</param>
    /// <returns>A disposable bridge host.</returns>
    public static AndroidProbeBridgeHost Start(Context context, IRemoteControlRootProvider rootProvider)
    {
        return StartAsync(context, rootProvider).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Starts the bridge host for the supplied Android context without blocking the caller thread.
    /// </summary>
    /// <param name="context">Android package context used to publish the marker file.</param>
    /// <param name="rootProvider">Avalonia root provider exposed to remote-control runtime services.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A disposable bridge host.</returns>
    public static Task<AndroidProbeBridgeHost> StartAsync(
        Context context,
        IRemoteControlRootProvider rootProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootProvider);

        return Task.Run(
            () => StartCoreAsync(context, rootProvider, cancellationToken),
            cancellationToken);
    }

    private static async Task<AndroidProbeBridgeHost> StartCoreAsync(
        Context context,
        IRemoteControlRootProvider rootProvider,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(rootProvider);
        cancellationToken.ThrowIfCancellationRequested();

        var debugToken = GenerateDebugToken();
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControlRuntime(options =>
        {
            options.IsEnabled = true;
            options.Host = IPAddress.Loopback;
            options.Port = DevicePort;
            options.RequireAuthentication = true;
            options.AuthenticationToken = debugToken;
            options.IsAdbTunnel = true;
            options.AllowRemoteActions = true;
            options.AllowRemoteFrames = true;
            options.AllowRemoteInput = true;
            options.AllowedMutableProperties.Add("Text");
        });
        services.AddSingleton(rootProvider);
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();
        try
        {
            var listener = serviceProvider.GetRequiredService<RemoteControlBridgeTcpListener>();
            await listener.StartAsync(cancellationToken).ConfigureAwait(false);

            var markerDirectory = context.FilesDir?.AbsolutePath
                ?? throw new InvalidOperationException("Android package files directory is unavailable.");
            await listener.CreateEndpointMarker()
                .WriteAsync(markerDirectory, cancellationToken)
                .ConfigureAwait(false);

            return new AndroidProbeBridgeHost(serviceProvider, listener);
        }
        catch
        {
            serviceProvider.Dispose();
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        listener.DisposeAsync().AsTask().GetAwaiter().GetResult();
        serviceProvider.Dispose();
    }

    private static string GenerateDebugToken()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    }
}
