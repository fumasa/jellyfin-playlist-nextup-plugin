using System;

namespace Jellyfin.Plugin.PlaylistNextUp.Services;

/// <summary>
/// Container types supported by the plugin.
/// </summary>
public enum ContainerType
{
    /// <summary>
    /// Playlist container.
    /// </summary>
    Playlist = 0,
    /// <summary>
    /// Collection (box set) container.
    /// </summary>
    Collection = 1
}

/// <summary>
/// Reference to a playlist or collection container.
/// </summary>
/// <param name="Type">Container type.</param>
/// <param name="Id">Container id.</param>
public sealed record ContainerRef(ContainerType Type, Guid Id);

/// <summary>
/// Stored playback progress for a container.
/// </summary>
/// <param name="UserId">User id.</param>
/// <param name="ContainerType">Container type.</param>
/// <param name="ContainerId">Container id.</param>
/// <param name="LastItemId">Last played item id.</param>
/// <param name="PositionTicks">Playback position in ticks.</param>
/// <param name="UpdatedAtUtc">Last update timestamp.</param>
public sealed record ProgressSnapshot(
    Guid UserId,
    ContainerType ContainerType,
    Guid ContainerId,
    Guid LastItemId,
    long PositionTicks,
    DateTimeOffset UpdatedAtUtc
);

/// <summary>
/// Resume candidate for a container.
/// </summary>
/// <param name="ContainerType">Container type.</param>
/// <param name="ContainerId">Container id.</param>
/// <param name="NextItemId">Next item id.</param>
/// <param name="ResumePositionTicks">Resume position in ticks.</param>
/// <param name="UpdatedAtUtc">Last update timestamp.</param>
public sealed record ResumeCandidate(
    ContainerType ContainerType,
    Guid ContainerId,
    Guid NextItemId,
    long ResumePositionTicks,
    DateTimeOffset UpdatedAtUtc
);
