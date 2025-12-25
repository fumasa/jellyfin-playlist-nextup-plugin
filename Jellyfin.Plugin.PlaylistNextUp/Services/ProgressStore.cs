using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaylistNextUp.Services;

/// <summary>
/// Small persistent store (JSON) saved under plugin config folder.
/// Replace with SQLite in v2.
/// </summary>
public sealed class ProgressStore
{
    private readonly ILogger<ProgressStore> _logger;
    private readonly string _path;
    private readonly ConcurrentDictionary<string, ProgressSnapshot> _map = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressStore"/> class.
    /// </summary>
    public ProgressStore(IApplicationPaths paths, ILogger<ProgressStore> logger)
    {
        _logger = logger;
        _path = Path.Combine(paths.PluginConfigurationsPath, "Jellyfin.Plugin.PlaylistNextUp", "progress.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        Load();
    }

    /// <summary>
    /// Inserts or updates a progress snapshot.
    /// </summary>
    public void Upsert(ProgressSnapshot snapshot)
    {
        var key = Key(snapshot.UserId, snapshot.ContainerType, snapshot.ContainerId);
        _map[key] = snapshot;
        Save();
    }

    /// <summary>
    /// Gets all snapshots for a user.
    /// </summary>
    public IReadOnlyCollection<ProgressSnapshot> GetAllForUser(Guid userId)
    {
        var list = new List<ProgressSnapshot>();
        foreach (var kv in _map)
        {
            if (kv.Value.UserId == userId)
                list.Add(kv.Value);
        }
        return list;
    }

    /// <summary>
    /// Tries to get a snapshot for a container.
    /// </summary>
    public bool TryGet(Guid userId, ContainerType type, Guid containerId, out ProgressSnapshot snapshot)
        => _map.TryGetValue(Key(userId, type, containerId), out snapshot!);

    private static string Key(Guid userId, ContainerType type, Guid containerId) => $"{userId}:{(int)type}:{containerId}";

    private void Load()
    {
        if (!File.Exists(_path))
            return;

        try
        {
            var json = File.ReadAllText(_path);
            var items = JsonSerializer.Deserialize<List<ProgressSnapshot>>(json) ?? new();
            foreach (var item in items)
            {
                _map[Key(item.UserId, item.ContainerType, item.ContainerId)] = item;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PlaylistNextUp] Could not load progress store, starting fresh.");
        }
    }

    private void Save()
    {
        try
        {
            var items = new List<ProgressSnapshot>(_map.Values);
            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PlaylistNextUp] Could not save progress store.");
        }
    }
}
