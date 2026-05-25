using System.Text.Json;

namespace Avalonia.RemoteControl.Client.Profiles;

/// <summary>
/// Stores the default connection profile in user-scoped application data.
/// </summary>
public sealed class FileRemoteControlProfileStore : IRemoteControlProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string profilePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileRemoteControlProfileStore"/> class.
    /// </summary>
    /// <param name="profilePath">Optional explicit profile path for tests or custom hosts.</param>
    public FileRemoteControlProfileStore(string? profilePath = null)
    {
        this.profilePath = string.IsNullOrWhiteSpace(profilePath)
            ? GetDefaultProfilePath()
            : profilePath;
    }

    /// <inheritdoc />
    public async Task<RemoteControlConnectionProfile?> LoadDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(profilePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(profilePath);
        return await JsonSerializer.DeserializeAsync<RemoteControlConnectionProfile>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveDefaultAsync(
        RemoteControlConnectionProfile profile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var directory = Path.GetDirectoryName(profilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(profilePath);
        await JsonSerializer.SerializeAsync(
            stream,
            profile,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task ForgetDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(profilePath))
        {
            File.Delete(profilePath);
        }

        return Task.CompletedTask;
    }

    private static string GetDefaultProfilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrWhiteSpace(root))
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(root, "Avalonia.RemoteControl", "connection-profile.json");
    }
}
