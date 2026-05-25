using Avalonia.RemoteControl.Client.Profiles;
using Avalonia.RemoteControl.Protocol;

namespace Avalonia.RemoteControl.Tests;

public sealed class RemoteControlProfileStoreTests
{
    [Fact]
    public async Task FileProfileStoreSavesLoadsAndForgetsDefaultProfile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Avalonia.RemoteControl.Tests", Guid.NewGuid().ToString("N"));
        var profilePath = Path.Combine(directory, "connection-profile.json");
        var store = new FileRemoteControlProfileStore(profilePath);
        var profile = new RemoteControlConnectionProfile
        {
            Endpoint = "http://127.0.0.1:47100",
            Token = "dev-token",
            CertificatePath = "C:\\certs\\remote-control.cer",
            AcceptedServerCertificateSha256Fingerprint =
                "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
            TransportProtocol = RemoteControlProtocol.AndroidBridgeTransportProtocol,
            UpdatedUtc = DateTimeOffset.Parse("2026-05-25T00:00:00Z"),
        };

        await store.SaveDefaultAsync(profile);
        var loaded = await store.LoadDefaultAsync();
        await store.ForgetDefaultAsync();
        var forgotten = await store.LoadDefaultAsync();

        Assert.True(File.Exists(profilePath) is false);
        Assert.NotNull(loaded);
        Assert.Equal(profile.Endpoint, loaded.Endpoint);
        Assert.Equal(profile.Token, loaded.Token);
        Assert.Equal(profile.CertificatePath, loaded.CertificatePath);
        Assert.Equal(
            profile.AcceptedServerCertificateSha256Fingerprint,
            loaded.AcceptedServerCertificateSha256Fingerprint);
        Assert.Equal(profile.TransportProtocol, loaded.TransportProtocol);
        Assert.Null(forgotten);
    }

    [Fact]
    public async Task FileProfileStoreDefaultsMissingTransportProtocolToGrpc()
    {
        var directory = Path.Combine(Path.GetTempPath(), "Avalonia.RemoteControl.Tests", Guid.NewGuid().ToString("N"));
        var profilePath = Path.Combine(directory, "connection-profile.json");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(
            profilePath,
            """
            {
              "Endpoint": "http://127.0.0.1:47100",
              "Token": "dev-token",
              "UpdatedUtc": "2026-05-25T00:00:00Z"
            }
            """);
        var store = new FileRemoteControlProfileStore(profilePath);

        var loaded = await store.LoadDefaultAsync();

        Assert.NotNull(loaded);
        Assert.Equal(RemoteControlProtocol.GrpcTransportProtocol, loaded.TransportProtocol);
    }
}
