using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Avalonia.RemoteControl.Client;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Avalonia.RemoteControl.Server.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlDesktopSessionTests
{
    [Fact]
    public async Task DesktopSessionReadsCapabilitiesFromHostedServer()
    {
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

        try
        {
            await host.StartAsync();

            using var session = RemoteControlDesktopSession.Create(host.BoundAddress!, "dev-token");
            var capabilities = await session.GetCapabilitiesAsync();

            Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
            Assert.True(capabilities.SupportsTreeSnapshots);
            Assert.True(capabilities.SupportsLogStreaming);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task DesktopSessionStreamsHostedLogs()
    {
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
        var logBuffer = provider.GetRequiredService<RemoteControlLogBuffer>();

        try
        {
            await host.StartAsync();
            logBuffer.Publish(new RemoteControlLogEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow,
                Level = LogLevel.Information,
                Category = "Hosted.Test",
                EventId = 42,
                Message = "streamed",
            });

            using var session = RemoteControlDesktopSession.Create(host.BoundAddress!, "dev-token");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var enumerator = session.WatchLogsAsync("Information", "Hosted", cts.Token)
                .GetAsyncEnumerator(cts.Token);

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal("streamed", enumerator.Current.Message);
            Assert.Equal("Hosted.Test", enumerator.Current.Category);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task DesktopSessionTrustsConfiguredTlsCertificate()
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"arc-{Guid.NewGuid():N}.pfx");
        var trustPath = Path.Combine(Path.GetTempPath(), $"arc-{Guid.NewGuid():N}.cer");
        const string certificatePassword = "test-password";

        var certificateBytes = CreateCertificateBytes(certificatePassword);
        await File.WriteAllBytesAsync(
            certificatePath,
            certificateBytes.Pfx);
        await File.WriteAllBytesAsync(
            trustPath,
            certificateBytes.Cert);

        var services = new ServiceCollection();
        services.AddAvaloniaRemoteControl(options =>
        {
            options.IsEnabled = true;
            options.Host = IPAddress.Loopback;
            options.Port = 0;
            options.AuthenticationToken = "dev-token";
            options.AllowCleartextForLoopbackOrAdb = false;
            options.TlsCertificatePath = certificatePath;
            options.TlsCertificatePassword = certificatePassword;
        });

        await using var provider = services.BuildServiceProvider();
        var host = provider.GetRequiredService<AvaloniaRemoteControlServerHost>();

        try
        {
            await host.StartAsync();

            using var session = RemoteControlDesktopSession.Create(
                host.BoundAddress!,
                "dev-token",
                trustPath);

            var capabilities = await session.GetCapabilitiesAsync();

            Assert.Equal(RemoteControlProtocol.DisplayVersion, capabilities.ProtocolVersion);
        }
        finally
        {
            await host.StopAsync();
            File.Delete(certificatePath);
            File.Delete(trustPath);
        }
    }

    private static (byte[] Pfx, byte[] Cert) CreateCertificateBytes(string password)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));

        return (
            certificate.Export(X509ContentType.Pfx, password),
            certificate.Export(X509ContentType.Cert));
    }
}
