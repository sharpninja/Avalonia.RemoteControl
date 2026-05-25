using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.RemoteControl.Server.Hosting;

/// <summary>
/// Provides Avalonia application lifetime integration helpers for Avalonia.RemoteControl.
/// </summary>
public static class AvaloniaRemoteControlApplicationLifetimeExtensions
{
    /// <summary>
    /// Starts the remote-control server when the Avalonia application starts and stops it when the application exits.
    /// </summary>
    /// <param name="lifetime">Avalonia controlled application lifetime.</param>
    /// <param name="services">Application service provider containing Avalonia.RemoteControl services.</param>
    /// <returns>A disposable registration that detaches the lifetime handlers.</returns>
    public static IDisposable AttachAvaloniaRemoteControl(
        this IControlledApplicationLifetime lifetime,
        IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(services);

        return new AvaloniaRemoteControlLifetimeRegistration(lifetime, services);
    }

    private sealed class AvaloniaRemoteControlLifetimeRegistration : IDisposable
    {
        private readonly IControlledApplicationLifetime lifetime;
        private readonly IServiceProvider services;
        private bool disposed;

        public AvaloniaRemoteControlLifetimeRegistration(
            IControlledApplicationLifetime lifetime,
            IServiceProvider services)
        {
            this.lifetime = lifetime;
            this.services = services;

            lifetime.Startup += OnStartup;
            lifetime.Exit += OnExit;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            lifetime.Startup -= OnStartup;
            lifetime.Exit -= OnExit;
            disposed = true;
        }

        private void OnStartup(
            object? sender,
            ControlledApplicationLifetimeStartupEventArgs e)
        {
            services
                .StartAvaloniaRemoteControlAsync()
                .GetAwaiter()
                .GetResult();
        }

        private void OnExit(
            object? sender,
            ControlledApplicationLifetimeExitEventArgs e)
        {
            services
                .StopAvaloniaRemoteControlAsync()
                .GetAwaiter()
                .GetResult();
        }
    }
}
