using System.Net;
using Avalonia.RemoteControl.Server.Commands;
using Avalonia.RemoteControl.Server.Grpc;
using Avalonia.RemoteControl.Server.Logging;
using Avalonia.RemoteControl.Server.Security;
using Avalonia.RemoteControl.Server.Snapshots;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Server.Hosting;

/// <summary>
/// Starts and stops the embeddable gRPC transport for Avalonia.RemoteControl.
/// </summary>
public sealed class AvaloniaRemoteControlServerHost : IAsyncDisposable
{
    private readonly IServiceProvider applicationServices;
    private readonly IOptions<AvaloniaRemoteControlOptions> options;
    private readonly RemoteControlStartupValidator startupValidator;
    private readonly ILogger<AvaloniaRemoteControlServerHost> logger;
    private WebApplication? app;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaRemoteControlServerHost"/> class.
    /// </summary>
    /// <param name="applicationServices">The debuggee application service provider.</param>
    /// <param name="options">Remote-control options.</param>
    /// <param name="startupValidator">Startup validation service.</param>
    /// <param name="logger">Transport logger.</param>
    public AvaloniaRemoteControlServerHost(
        IServiceProvider applicationServices,
        IOptions<AvaloniaRemoteControlOptions> options,
        RemoteControlStartupValidator startupValidator,
        ILogger<AvaloniaRemoteControlServerHost> logger)
    {
        this.applicationServices = applicationServices;
        this.options = options;
        this.startupValidator = startupValidator;
        this.logger = logger;
    }

    /// <summary>
    /// Gets the bound address after startup, or <see langword="null" /> when the server is stopped.
    /// </summary>
    public Uri? BoundAddress { get; private set; }

    /// <summary>
    /// Starts the gRPC transport if the remote-control server is enabled.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing startup.</returns>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (app is not null)
        {
            return;
        }

        var currentOptions = options.Value;

        if (!currentOptions.IsEnabled)
        {
            logger.LogInformation("Avalonia.RemoteControl server is disabled.");
            return;
        }

        var validation = startupValidator.Validate();

        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                "Avalonia.RemoteControl startup validation failed: "
                + string.Join(" ", validation.Errors));
        }

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(AvaloniaRemoteControlServerHost).Assembly.FullName,
        });

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.Listen(currentOptions.Host, currentOptions.Port, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;

                if (ShouldUseTls(currentOptions))
                {
                    listenOptions.UseHttps(
                        currentOptions.TlsCertificatePath!,
                        currentOptions.TlsCertificatePassword);
                }
            });
        });

        RegisterApplicationServices(builder.Services);

        app = builder.Build();
        app.MapGrpcService<AvaloniaRemoteControlGrpcService>();

        await app.StartAsync(cancellationToken).ConfigureAwait(false);
        BoundAddress = ResolveBoundAddress(app.Services, currentOptions);

        logger.LogInformation(
            "Avalonia.RemoteControl server started on {Address}",
            BoundAddress);
    }

    /// <summary>
    /// Stops the gRPC transport if it is running.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing shutdown.</returns>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (app is null)
        {
            return;
        }

        await app.StopAsync(cancellationToken).ConfigureAwait(false);
        await app.DisposeAsync().ConfigureAwait(false);
        app = null;
        BoundAddress = null;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }

    private void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton(options);
        services.AddGrpc(grpcOptions =>
        {
            grpcOptions.Interceptors.Add<RemoteControlAuthenticationInterceptor>();
        });

        services.AddSingleton(applicationServices.GetRequiredService<AvaloniaRemoteControlService>());
        services.AddSingleton(applicationServices.GetRequiredService<AvaloniaRemoteControlGrpcService>());
        services.AddSingleton(applicationServices.GetRequiredService<RemoteControlAuthenticationInterceptor>());
        services.AddSingleton(applicationServices.GetRequiredService<IRemoteControlRootProvider>());
        services.AddSingleton(applicationServices.GetRequiredService<IControlTreeSnapshotProvider>());
        services.AddSingleton(applicationServices.GetRequiredService<RemoteControlTreeStreamService>());
        services.AddSingleton(applicationServices.GetRequiredService<RemoteControlLogStreamService>());
        services.AddSingleton(applicationServices.GetRequiredService<RemoteControlActionInvoker>());
        services.AddSingleton(applicationServices.GetRequiredService<RemoteControlPropertyMutationService>());
    }

    private static bool ShouldUseTls(AvaloniaRemoteControlOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.TlsCertificatePath)
            && (!IPAddress.IsLoopback(options.Host) || !options.AllowCleartextForLoopbackOrAdb);
    }

    private static Uri ResolveBoundAddress(
        IServiceProvider services,
        AvaloniaRemoteControlOptions options)
    {
        var server = services.GetService<IServer>();
        var address = server?.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(address))
        {
            return new Uri(address);
        }

        var scheme = ShouldUseTls(options) ? "https" : "http";
        var host = options.Host.Equals(IPAddress.Any) || options.Host.Equals(IPAddress.IPv6Any)
            ? "localhost"
            : options.Host.ToString();

        return new Uri($"{scheme}://{host}:{options.Port}");
    }
}
