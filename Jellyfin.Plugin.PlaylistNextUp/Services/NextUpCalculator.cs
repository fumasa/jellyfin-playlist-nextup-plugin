using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Playlists;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaylistNextUp.Services;

/// <summary>
/// Computes the best next item for a given container.
/// </summary>
public sealed class NextUpCalculator
{
    private readonly ILogger<NextUpCalculator> _logger;
    private readonly ILibraryManager _libraryManager;
    private readonly IPlaylistManager _playlistManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextUpCalculator"/> class.
    /// </summary>
    public NextUpCalculator(ILibraryManager libraryManager, IPlaylistManager playlistManager, ILogger<NextUpCalculator> logger)
    {
        _libraryManager = libraryManager;
        _playlistManager = playlistManager;
        _logger = logger;
    }

    /// <summary>
    /// Builds a progress snapshot from a playback event.
    /// </summary>
    public ProgressSnapshot BuildSnapshot(ContainerRef container, MediaBrowser.Controller.Library.PlaybackProgressEventArgs e)
    {
        PlaybackEventHelper.TryGetGuid(e, "UserId", out var userId);
        PlaybackEventHelper.TryGetGuid(e, "ItemId", out var itemId);

        return new ProgressSnapshot(
            userId,
            container.Type,
            container.Id,
            itemId,
            PlaybackEventHelper.TryGetLong(e, "PlaybackPositionTicks", out var positionTicks) ? positionTicks : 0,
            DateTimeOffset.UtcNow
        );
    }

    /// <summary>
    /// Computes the resume candidate for a container snapshot.
    /// </summary>
    public ResumeCandidate? ComputeResume(Guid userId, ProgressSnapshot snapshot)
    {
        try
        {
            // Get ordered container items
            var cfg = Plugin.Instance?.Configuration;
            var items = GetOrderedItems(snapshot.ContainerType, snapshot.ContainerId, cfg);
            if (items.Count == 0) return null;

            // Find current index
            var idx = items.FindIndex(i => i.Id == snapshot.LastItemId);
            if (idx < 0)
            {
                // If last item missing, resume from first
                var first = items[0];
                return new ResumeCandidate(snapshot.ContainerType, snapshot.ContainerId, first.Id, 0, snapshot.UpdatedAtUtc);
            }

            // If we are mid-item, keep same item resume position.
            var resumeTicks = snapshot.PositionTicks;
            var current = items[idx];

            // If near end, advance to next item.
            if (cfg != null && IsCompleted(current, resumeTicks, cfg.CompletedPercent))
            {
                var nextIdx = Math.Min(idx + 1, items.Count - 1);
                var next = items[nextIdx];
                return new ResumeCandidate(snapshot.ContainerType, snapshot.ContainerId, next.Id, 0, snapshot.UpdatedAtUtc);
            }

            return new ResumeCandidate(snapshot.ContainerType, snapshot.ContainerId, current.Id, resumeTicks, snapshot.UpdatedAtUtc);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[PlaylistNextUp] ComputeResume failed.");
            return null;
        }
    }

    private List<BaseItem> GetOrderedItems(ContainerType type, Guid containerId, PluginConfiguration? cfg)
    {
        if (type == ContainerType.Playlist)
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                ParentId = containerId,
                Recursive = true
            });
        }

        // Collections (BoxSet)
        var box = _libraryManager.GetItemById(containerId);
        if (box is null) return new();

        var children = _libraryManager.GetItemList(new InternalItemsQuery
        {
            ParentId = box.Id,
            Recursive = true
        });

        return OrderCollectionItems(children, cfg?.CollectionOrdering ?? CollectionOrdering.ReleaseDate);
    }

    private static List<BaseItem> OrderCollectionItems(IEnumerable<BaseItem> items, CollectionOrdering ordering)
    {
        return ordering switch
        {
            CollectionOrdering.PremiereDate => items
                .OrderBy(i => i.PremiereDate ?? DateTime.MinValue)
                .ThenBy(i => i.SortName)
                .ToList(),
            CollectionOrdering.SortName => items
                .OrderBy(i => i.SortName)
                .ToList(),
            CollectionOrdering.ProductionYear => items
                .OrderBy(i => i.ProductionYear ?? 0)
                .ThenBy(i => i.SortName)
                .ToList(),
            _ => items
                .OrderBy(i => i.PremiereDate ?? DateTime.MinValue)
                .ThenBy(i => i.SortName)
                .ToList()
        };
    }

    private static bool IsCompleted(BaseItem item, long positionTicks, int completedPercent)
    {
        if (item.RunTimeTicks is null || item.RunTimeTicks <= 0) return false;

        var pct = (double)positionTicks / item.RunTimeTicks.Value * 100.0;
        return pct >= completedPercent;
    }
}
