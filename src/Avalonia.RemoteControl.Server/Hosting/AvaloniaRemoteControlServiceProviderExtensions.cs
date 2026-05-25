using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Server.Hosting;

/// <summary>
/// Provides service-provider startup helpers for Avalonia.RemoteControl.
/// </summary>
public static class AvaloniaRemoteControlServiceProviderExtensions
{
    /// <summary>
    /// Starts the configured remote-control server host if enabled.
    /// </summary>
    /// <param name="services">Application service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing startup.</returns>
    public static Task StartAvaloniaRemoteControlAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .GetRequiredService<AvaloniaRemoteControlServerHost>()
            .StartAsync(cancellationToken);
    }

    /// <summary>
    /// Stops the configured remote-control server host if running.
    /// </summary>
    /// <param name="services">Application service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing shutdown.</returns>
    public static Task StopAvaloniaRemoteControlAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .GetRequiredService<AvaloniaRemoteControlServerHost>()
            .StopAsync(cancellationToken);
    }
}
