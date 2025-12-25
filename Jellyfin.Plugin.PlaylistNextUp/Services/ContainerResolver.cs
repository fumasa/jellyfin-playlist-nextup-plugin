using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaylistNextUp.Services;

/// <summary>
/// Best-effort resolver that tries to find which playlist/collection this playback belongs to.
/// </summary>
public sealed class ContainerResolver
{
    private readonly ILogger<ContainerResolver> _logger;

    // NOTE: These services exist in Jellyfin, but exact interfaces may vary across versions.
    // If compilation fails against 10.10.3 packages, adjust the injected types to match.
    private readonly IUserViewManager _userViewManager;
    private readonly IPlaylistManager _playlistManager;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContainerResolver"/> class.
    /// </summary>
    public ContainerResolver(
        IUserViewManager userViewManager,
        IPlaylistManager playlistManager,
        ILibraryManager libraryManager,
        ILogger<ContainerResolver> logger)
    {
        _userViewManager = userViewManager;
        _playlistManager = playlistManager;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Resolves playlist/collection containers for a playback event.
    /// </summary>
    public List<ContainerRef> ResolveContainersForPlayback(PlaybackProgressEventArgs e)
    {
        var cfg = Plugin.Instance?.Configuration;
        var result = new List<ContainerRef>();
        var hasUserId = PlaybackEventHelper.TryGetGuid(e, "UserId", out var userId);
        var hasItemId = PlaybackEventHelper.TryGetGuid(e, "ItemId", out var itemId);

        // Heuristic #0: try direct extraction of playlist/collection ids from event args.
        if (TryResolveFromEventArgs(e, result))
            return result;

        // Heuristic #1: look for playlists containing the item.
        if (cfg?.TrackPlaylists == true && hasItemId && hasUserId)
        {
            try
            {
                // Many implementations allow enumerating user playlists and checking items.
                // We keep this conservative: only attempt if playlist manager can list.
                var playlists = _playlistManager.GetPlaylists(userId);
                foreach (var pl in playlists)
                {
                    if (pl == null) continue;
                    var children = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        ParentId = pl.Id,
                        Recursive = true
                    });
                    if (children.Any(i => i.Id == itemId))
                        result.Add(new ContainerRef(ContainerType.Playlist, pl.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[PlaylistNextUp] Playlist membership detection failed.");
            }
        }

        // Heuristic #2: collections (box sets) containing the item.
        if (cfg?.TrackCollections == true && hasItemId)
        {
            try
            {
                // Collections are typically BoxSet items.
                // Find parents of the item that are BoxSet.
                var item = _libraryManager.GetItemById(itemId);
                if (item != null)
                {
                    var parents = _libraryManager.GetCollectionFolders(item);
                    foreach (var parent in parents)
                    {
                        if (parent is BoxSet boxSet)
                        {
                            result.Add(new ContainerRef(ContainerType.Collection, boxSet.Id));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[PlaylistNextUp] Collection membership detection failed.");
            }
        }

        return result;
    }

    private bool TryResolveFromEventArgs(PlaybackProgressEventArgs e, List<ContainerRef> result)
    {
        if (PlaybackEventHelper.TryGetGuid(e, "PlaylistId", out var playlistId))
        {
            AddIfMissing(result, new ContainerRef(ContainerType.Playlist, playlistId));
            return true;
        }

        if (PlaybackEventHelper.TryGetGuid(e, "CollectionId", out var collectionId)
            || PlaybackEventHelper.TryGetGuid(e, "BoxSetId", out collectionId))
        {
            AddIfMissing(result, new ContainerRef(ContainerType.Collection, collectionId));
            return true;
        }

        if (PlaybackEventHelper.TryGetGuid(e, "ParentId", out var parentId))
        {
            if (PlaybackEventHelper.TryGetGuid(e, "UserId", out var userId))
            {
                try
                {
                    var playlists = _playlistManager.GetPlaylists(userId);
                    if (playlists.Any(pl => pl.Id == parentId))
                    {
                        AddIfMissing(result, new ContainerRef(ContainerType.Playlist, parentId));
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "[PlaylistNextUp] ParentId playlist lookup failed.");
                }
            }

            try
            {
                var parent = _libraryManager.GetItemById(parentId);
                if (parent is BoxSet boxSet)
                {
                    AddIfMissing(result, new ContainerRef(ContainerType.Collection, boxSet.Id));
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[PlaylistNextUp] ParentId collection lookup failed.");
            }
        }

        return result.Count > 0;
    }

    private static void AddIfMissing(List<ContainerRef> result, ContainerRef container)
    {
        if (!result.Any(r => r.Type == container.Type && r.Id == container.Id))
            result.Add(container);
    }
}
