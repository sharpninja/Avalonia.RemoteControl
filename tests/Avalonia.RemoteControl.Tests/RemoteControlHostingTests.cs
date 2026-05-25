using System.Net;
using System.Reflection;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlHostingTests
{
    [Fact]
    public async Task ServiceProviderStartupHelperDoesNotBindWhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl();

        await using var provider = services.BuildServiceProvider();

        await provider.StartAvaloniaRemoteControlAsync();
        var host = provider.GetRequiredService<AvaloniaRemoteControlServerHost>();
        await provider.StopAvaloniaRemoteControlAsync();

        Assert.Null(host.BoundAddress);
    }

    [Fact]
    public async Task ControlledApplicationLifetimeHelperStartsAndStopsServer()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl(options =>
        {
            options.IsEnabled = true;
            options.Host = IPAddress.Loopback;
            options.Port = 0;
            options.AuthenticationToken = "dev-token";
        });

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<AvaloniaRemoteControlServerHost>();
        var lifetime = new ClassicDesktopStyleApplicationLifetime();

        try
        {
            using var registration = lifetime.AttachAvaloniaRemoteControl(provider);

            RaiseLifetimeEvent(lifetime, "Startup");

            Assert.NotNull(host.BoundAddress);

            RaiseLifetimeEvent(lifetime, "Exit");

            Assert.Null(host.BoundAddress);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task ControlledApplicationLifetimeRegistrationDetachesWhenDisposed()
    {
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl(options =>
        {
            options.IsEnabled = true;
            options.Host = IPAddress.Loopback;
            options.Port = 0;
            options.AuthenticationToken = "dev-token";
        });

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<AvaloniaRemoteControlServerHost>();
        var lifetime = new ClassicDesktopStyleApplicationLifetime();

        try
        {
            using (lifetime.AttachAvaloniaRemoteControl(provider))
            {
            }

            RaiseLifetimeEvent(lifetime, "Startup");

            Assert.Null(host.BoundAddress);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    private static void RaiseLifetimeEvent<TLifetime>(TLifetime lifetime, string eventName)
        where TLifetime : class
    {
        var field = typeof(TLifetime).GetField(
            eventName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        var handler = field?.GetValue(lifetime) as MulticastDelegate;

        if (handler is null)
        {
            return;
        }

        foreach (var invocation in handler.GetInvocationList())
        {
            invocation.DynamicInvoke(lifetime, null);
        }
    }
}
