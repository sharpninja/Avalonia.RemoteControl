using System.Collections.ObjectModel;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Serializer.SystemTextJson;

namespace Avalonia.RemoteControl.Tool.Docking;

/// <summary>
/// Persists and restores the Dock.Avalonia layout tree as System.Text.Json under the tool's
/// per-profile settings directory. Panel view-models (dockable Content) are re-attached from the
/// live shell by the factory in InitLayout, not serialized.
/// </summary>
public sealed class RemoteControlDockLayoutStore
{
    private readonly string _rootPath;
    private readonly IDockSerializer _serializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteControlDockLayoutStore"/> class.
    /// </summary>
    /// <param name="rootPath">Directory for layout files; defaults to the per-profile projects directory.</param>
    public RemoteControlDockLayoutStore(string? rootPath = null)
    {
        _rootPath = string.IsNullOrWhiteSpace(rootPath) ? DefaultRootPath() : rootPath;
        _serializer = new DockSerializer(typeof(ObservableCollection<>));
    }

    /// <summary>
    /// Gets the layout file path for a project id.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <returns>The full layout file path.</returns>
    public string GetLayoutPath(string projectId)
        => Path.Combine(_rootPath, projectId + ".arclayout.json");

    /// <summary>
    /// Saves the dock layout tree for a project.
    /// </summary>
    /// <param name="layout">Root dock to save.</param>
    /// <param name="projectId">Project identifier.</param>
    public void Save(IRootDock layout, string projectId)
    {
        ArgumentNullException.ThrowIfNull(layout);
        Directory.CreateDirectory(_rootPath);

        // Owner is a serialized ([DataMember]) back-reference that the factory rebuilds via InitLayout on
        // load; leaving it set makes System.Text.Json walk the owner chain past its depth limit. Detach it
        // (and the captured owners) for serialization, then restore so the live layout keeps working.
        var owners = DetachOwners(layout);
        byte[] payload;
        try
        {
            // Serialize to memory first so a serialization failure never truncates the last good layout file.
            using var buffer = new MemoryStream();
            _serializer.Save(buffer, layout);
            payload = buffer.ToArray();
        }
        finally
        {
            RestoreOwners(owners);
        }

        File.WriteAllBytes(GetLayoutPath(projectId), payload);
    }

    /// <summary>
    /// Loads the dock layout tree for a project and re-attaches panel view-models via the factory.
    /// </summary>
    /// <param name="projectId">Project identifier.</param>
    /// <param name="factory">Factory used to re-initialize the loaded layout (Content re-attach + locators).</param>
    /// <returns>The restored root dock, or <see langword="null"/> when no layout file exists.</returns>
    public IRootDock? Load(string projectId, IFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        var path = GetLayoutPath(projectId);
        if (!File.Exists(path))
        {
            return null;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        var layout = _serializer.Load<IRootDock?>(stream);
        if (layout is null)
        {
            return null;
        }

        factory.InitLayout(layout);
        return layout;
    }

    private static List<(IDockable Dockable, IDockable? Owner)> DetachOwners(IDockable root)
    {
        var owners = new List<(IDockable, IDockable?)>();
        foreach (var dockable in Flatten(root))
        {
            owners.Add((dockable, dockable.Owner));
            dockable.Owner = null;
        }

        return owners;
    }

    private static void RestoreOwners(List<(IDockable Dockable, IDockable? Owner)> owners)
    {
        foreach (var (dockable, owner) in owners)
        {
            dockable.Owner = owner;
        }
    }

    private static IEnumerable<IDockable> Flatten(IDockable dockable)
    {
        yield return dockable;
        if (dockable is IDock dock && dock.VisibleDockables is { } children)
        {
            foreach (var child in children)
            {
                foreach (var nested in Flatten(child))
                {
                    yield return nested;
                }
            }
        }
    }

    private static string DefaultRootPath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Avalonia.RemoteControl",
            "projects");
}
