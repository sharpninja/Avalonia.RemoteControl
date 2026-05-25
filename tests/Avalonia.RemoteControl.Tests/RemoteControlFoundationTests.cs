using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlFoundationTests
{
    [Fact]
    public void ServerOptionsDefaultToDisabledAndAuthenticated()
    {
        var options = new AvaloniaRemoteControlOptions();

        Assert.False(options.IsEnabled);
        Assert.True(options.RequireAuthentication);
        Assert.True(options.RequireTlsForNonLoopback);
        Assert.True(options.DenyPropertyMutationByDefault);
        Assert.False(options.AllowRemoteActions);
        Assert.Contains("token", options.SensitiveNameFragments);
    }

    [Fact]
    public void ServiceCollectionRegistersServerServicesWithoutEnablingServer()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAvaloniaRemoteControl();

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<AvaloniaRemoteControlService>();

        var state = service.GetStartupState();

        Assert.False(state.IsEnabled);
        Assert.True(state.RequiresAuthentication);
    }

    [Fact]
    public void StartupStateDoesNotExposeSecrets()
    {
        var service = new AvaloniaRemoteControlService(
            Options.Create(new AvaloniaRemoteControlOptions { IsEnabled = true }),
            NullLogger<AvaloniaRemoteControlService>.Instance);

        var state = service.GetStartupState();

        Assert.True(state.IsEnabled);
        Assert.DoesNotContain("token", state.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClientHelpIncludesAdbWorkflowAndProtocolVersion()
    {
        var help = RemoteControlClientInfo.CreateHelpText();

        Assert.Contains(RemoteControlClientInfo.CommandName, help);
        Assert.Contains("adb connect", help);
        Assert.Contains(RemoteControlProtocol.DisplayVersion, help);
    }

    [Fact]
    public void ClientDeclaresRequiredConnectionModes()
    {
        Assert.Contains(RemoteControlConnectionMode.Local, RemoteControlClientInfo.SupportedConnectionModes);
        Assert.Contains(RemoteControlConnectionMode.Network, RemoteControlClientInfo.SupportedConnectionModes);
        Assert.Contains(RemoteControlConnectionMode.Adb, RemoteControlClientInfo.SupportedConnectionModes);
    }
}
