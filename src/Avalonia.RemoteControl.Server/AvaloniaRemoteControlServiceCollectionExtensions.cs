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

        services.AddAvaloniaRemoteControlRuntime(configure);
        services.AddGrpc();
        services.AddSingleton<Grpc.AvaloniaRemoteControlGrpcService>(provider =>
            new Grpc.AvaloniaRemoteControlGrpcService(
                provider.GetRequiredService<Runtime.IRemoteControlRuntime>()));
        services.AddSingleton<Security.RemoteControlAuthenticationInterceptor>(provider =>
            new Security.RemoteControlAuthenticationInterceptor(
                provider.GetRequiredService<Security.RemoteControlBearerTokenAuthenticator>(),
                provider.GetRequiredService<ILogger<Security.RemoteControlAuthenticationInterceptor>>()));
        services.AddSingleton<Hosting.AvaloniaRemoteControlServerHost>();

        return services;
    }
}
