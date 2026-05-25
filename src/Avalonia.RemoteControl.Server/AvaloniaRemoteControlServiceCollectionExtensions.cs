using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.RemoteControl.Server;

/// <summary>
/// Registers Avalonia.RemoteControl server services with an application service provider.
/// </summary>
public static class AvaloniaRemoteControlServiceCollectionExtensions
{
    /// <summary>
    /// Adds the remote-control server services. The server remains disabled unless configured otherwise.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">Optional configuration callback.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddAvaloniaRemoteControl(
        this IServiceCollection services,
        Action<AvaloniaRemoteControlOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is null)
        {
            services.AddOptions<AvaloniaRemoteControlOptions>();
        }
        else
        {
            services.AddOptions<AvaloniaRemoteControlOptions>().Configure(configure);
        }

        services.AddGrpc();
        services.AddLogging();
        services.AddSingleton<AvaloniaRemoteControlService>();
        services.AddSingleton<Grpc.AvaloniaRemoteControlGrpcService>();
        services.AddSingleton<IRemoteControlRootProvider, EmptyRemoteControlRootProvider>();
        services.AddSingleton<Logging.RemoteControlLogBuffer>();
        services.AddSingleton<Logging.RemoteControlLogStreamService>();
        services.AddSingleton<Logging.RemoteControlLoggerProvider>();
        services.AddSingleton<ILoggerProvider>(provider =>
            provider.GetRequiredService<Logging.RemoteControlLoggerProvider>());
        services.AddSingleton<Commands.RemoteControlActionInvoker>();
        services.AddSingleton<Commands.RemoteControlPropertyMutationService>();
        services.AddSingleton<Threading.IRemoteControlDispatcher, Threading.AvaloniaUiThreadRemoteControlDispatcher>();
        services.AddSingleton<Snapshots.RemoteControlTreeStreamService>();
        services.AddSingleton<Snapshots.AvaloniaControlTreeSnapshotProvider>();
        services.AddSingleton<Snapshots.IControlTreeSnapshotProvider>(provider =>
            provider.GetRequiredService<Snapshots.AvaloniaControlTreeSnapshotProvider>());
        services.AddSingleton<Snapshots.IRemoteControlNodeResolver>(provider =>
            provider.GetRequiredService<Snapshots.AvaloniaControlTreeSnapshotProvider>());

        return services;
    }
}
