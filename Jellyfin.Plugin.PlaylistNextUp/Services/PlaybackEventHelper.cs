using System;
using System.Reflection;
using MediaBrowser.Controller.Library;

namespace Jellyfin.Plugin.PlaylistNextUp.Services;

/// <summary>
/// Helper methods to read playback event properties across Jellyfin versions.
/// </summary>
public static class PlaybackEventHelper
{
    /// <summary>
    /// Tries to read a Guid property from a playback event.
    /// </summary>
    public static bool TryGetGuid(PlaybackProgressEventArgs e, string propertyName, out Guid value)
        => TryGetGuidProperty(e, propertyName, out value);

    /// <summary>
    /// Tries to read a long property from a playback event.
    /// </summary>
    public static bool TryGetLong(PlaybackProgressEventArgs e, string propertyName, out long value)
        => TryGetLongProperty(e, propertyName, out value);

    private static bool TryGetGuidProperty(object source, string propertyName, out Guid value)
    {
        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (prop is null)
        {
            value = Guid.Empty;
            return false;
        }

        var raw = prop.GetValue(source);
        if (raw is Guid g)
        {
            value = g;
            return value != Guid.Empty;
        }

        if (raw != null && Guid.TryParse(raw.ToString(), out var parsed))
        {
            value = parsed;
            return value != Guid.Empty;
        }

        value = Guid.Empty;
        return false;
    }

    private static bool TryGetLongProperty(object source, string propertyName, out long value)
    {
        var prop = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        if (prop is null)
        {
            value = 0;
            return false;
        }

        var raw = prop.GetValue(source);
        if (raw is long l)
        {
            value = l;
            return true;
        }

        if (raw is int i)
        {
            value = i;
            return true;
        }

        if (raw != null && long.TryParse(raw.ToString(), out var parsed))
        {
            value = parsed;
            return true;
        }

        value = 0;
        return false;
    }
}
