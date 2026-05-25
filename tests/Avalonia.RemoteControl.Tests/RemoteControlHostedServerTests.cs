using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Avalonia.RemoteControl.Protocol;
using Avalonia.RemoteControl.Protocol.V1;
using Avalonia.RemoteControl.Server;
using Avalonia.RemoteControl.Server.Hosting;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlHostedServerTests
{
    [Fact]
    public async Task HostedGrpcServerRequiresBearerTokenAndServesAuthenticatedRequests()
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

        try
        {
            await host.StartAsync();

            Assert.NotNull(host.BoundAddress);

            using var channel = GrpcChannel.ForAddress(host.BoundAddress!);
            var client = new Protocol.V1.RemoteControl.RemoteControlClient(channel);

            var unauthenticated = await Assert.ThrowsAsync<RpcException>(async () =>
                await client.GetCapabilitiesAsync(new GetCapabilitiesRequest()));

            Assert.Equal(StatusCode.Unauthenticated, unauthenticated.StatusCode);

            var authenticated = await client.GetCapabilitiesAsync(
                new GetCapabilitiesRequest(),
                new global::Grpc.Core.Metadata { { "authorization", "Bearer dev-token" } });

            Assert.Equal(RemoteControlProtocol.DisplayVersion, authenticated.ProtocolVersion);
            Assert.True(authenticated.SupportsTreeSnapshots);
            Assert.True(authenticated.SupportsLogStreaming);
        }
        finally
        {
            await host.StopAsync();
        }
    }

    [Fact]
    public async Task HostedGrpcServerSupportsTlsWhenCleartextLoopbackIsDisabled()
    {
        var certificatePath = Path.Combine(Path.GetTempPath(), $"arc-{Guid.NewGuid():N}.pfx");
        const string certificatePassword = "test-password";

        await File.WriteAllBytesAsync(
            certificatePath,
            CreateCertificateBytes(certificatePassword));

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

            Assert.NotNull(host.BoundAddress);
            Assert.Equal("https", host.BoundAddress!.Scheme);

            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            };
            using var channel = GrpcChannel.ForAddress(
                host.BoundAddress!,
                new GrpcChannelOptions { HttpHandler = handler });
            var client = new Protocol.V1.RemoteControl.RemoteControlClient(channel);

            var authenticated = await client.GetCapabilitiesAsync(
                new GetCapabilitiesRequest(),
                new global::Grpc.Core.Metadata { { "authorization", "Bearer dev-token" } });

            Assert.Equal(RemoteControlProtocol.DisplayVersion, authenticated.ProtocolVersion);
        }
        finally
        {
            await host.StopAsync();
            File.Delete(certificatePath);
        }
    }

    private static byte[] CreateCertificateBytes(string password)
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

        return certificate.Export(X509ContentType.Pfx, password);
    }
}
