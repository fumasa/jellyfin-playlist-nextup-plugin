using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PlaylistNextUp;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether playlists are tracked.
    /// </summary>
    public bool TrackPlaylists { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether collections are tracked.
    /// </summary>
    public bool TrackCollections { get; set; } = true;

    /// <summary>
    /// Minimum seconds watched before we store progress.
    /// </summary>
    public int ResumeThresholdSeconds { get; set; } = 60;

    /// <summary>
    /// After this percentage we consider the item completed for advancing.
    /// </summary>
    public int CompletedPercent { get; set; } = 92;

    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled.
    /// </summary>
    public bool DebugLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the collection ordering.
    /// </summary>
    public CollectionOrdering CollectionOrdering { get; set; } = CollectionOrdering.ReleaseDate;

    /// <summary>
    /// Optional allowlist of user ids. Empty means all users.
    /// </summary>
    public string[] AllowedUserIds { get; set; } = [];
}

/// <summary>
/// Collection ordering options.
/// </summary>
public enum CollectionOrdering
{
    /// <summary>
    /// Order by release date.
    /// </summary>
    ReleaseDate = 0,
    /// <summary>
    /// Order by premiere date.
    /// </summary>
    PremiereDate = 1,
    /// <summary>
    /// Order by sort name.
    /// </summary>
    SortName = 2,
    /// <summary>
    /// Order by production year.
    /// </summary>
    ProductionYear = 3
}
