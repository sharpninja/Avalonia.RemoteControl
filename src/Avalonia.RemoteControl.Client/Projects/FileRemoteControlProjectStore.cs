using System.Text.Json;

namespace Avalonia.RemoteControl.Client.Projects;

/// <summary>
/// Stores remote-control projects as user-scoped versioned JSON documents.
/// </summary>
public sealed class FileRemoteControlProjectStore : IRemoteControlProjectStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string rootPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileRemoteControlProjectStore"/> class.
    /// </summary>
    /// <param name="rootPath">Optional explicit root path for tests or custom hosts.</param>
    public FileRemoteControlProjectStore(string? rootPath = null)
    {
        this.rootPath = string.IsNullOrWhiteSpace(rootPath)
            ? GetDefaultProjectRoot()
            : rootPath;
    }

    /// <summary>
    /// Gets the root directory where project documents are stored.
    /// </summary>
    public string RootPath => rootPath;

    /// <inheritdoc />
    public async Task<RemoteControlProjectDocument?> LoadAsync(
        string projectId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectId);

        var path = GetProjectPath(projectId);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<RemoteControlProjectDocument>(
            stream,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        RemoteControlProjectDocument document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(document.ProjectId);

        Directory.CreateDirectory(rootPath);
        document.UpdatedUtc = DateTimeOffset.UtcNow;

        var path = GetProjectPath(document.ProjectId);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(
            stream,
            document,
            JsonOptions,
            cancellationToken).ConfigureAwait(false);
    }

    private string GetProjectPath(string projectId)
    {
        return Path.Combine(rootPath, $"{SanitizeFileName(projectId)}.arcproj.json");
    }

    private static string GetDefaultProjectRoot()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        if (string.IsNullOrWhiteSpace(root))
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        return Path.Combine(root, "Avalonia.RemoteControl", "projects");
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = value
            .Trim()
            .Select(character => invalid.Contains(character) ? '_' : character)
            .ToArray();

        return new string(chars);
    }
}
