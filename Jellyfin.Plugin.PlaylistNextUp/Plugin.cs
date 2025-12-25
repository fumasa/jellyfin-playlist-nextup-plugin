using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.PlaylistNextUp;

/// <summary>
/// Plugin entry.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes the plugin instance.
    /// </summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the singleton plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <summary>
    /// Gets the plugin display name.
    /// </summary>
    public override string Name => "Playlist/Collection Next Up";

    /// <summary>
    /// Gets the plugin unique identifier.
    /// </summary>
    public override Guid Id => Guid.Parse("f5c1f45c-6f5a-4b25-8b1f-4d5c60d9a2c6");

    /// <summary>
    /// Gets the dashboard pages for the plugin.
    /// </summary>
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "playlistnextup",
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}
