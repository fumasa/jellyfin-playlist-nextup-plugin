using System;
using MediaBrowser.Controller.Session;
using Jellyfin.Plugin.PlaylistNextUp.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.PlaylistNextUp.EntryPoints;

/// <summary>
/// Hooks into playback events and writes progress snapshots.
/// </summary>
public sealed class PlaybackMonitorEntryPoint : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly ILogger<PlaybackMonitorEntryPoint> _logger;
    private readonly Services.ProgressStore _store;
    private readonly Services.ContainerResolver _resolver;
    private readonly Services.NextUpCalculator _calculator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaybackMonitorEntryPoint"/> class.
    /// </summary>
    public PlaybackMonitorEntryPoint(
        ISessionManager sessionManager,
        ILogger<PlaybackMonitorEntryPoint> logger,
        Services.ProgressStore store,
        Services.ContainerResolver resolver,
        Services.NextUpCalculator calculator)
    {
        _sessionManager = sessionManager;
        _logger = logger;
        _store = store;
        _resolver = resolver;
        _calculator = calculator;
    }

    /// <summary>
    /// Starts the playback monitor.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart += OnPlaybackStart;
        _sessionManager.PlaybackProgress += OnPlaybackProgress;
        _sessionManager.PlaybackStopped += OnPlaybackStopped;

        if (Plugin.Instance?.Configuration.DebugLogging == true)
        {
            _logger.LogInformation("[PlaylistNextUp] Playback monitor started.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stops the playback monitor.
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStart -= OnPlaybackStart;
        _sessionManager.PlaybackProgress -= OnPlaybackProgress;
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;

        if (Plugin.Instance?.Configuration.DebugLogging == true)
        {
            _logger.LogInformation("[PlaylistNextUp] Playback monitor stopped.");
        }

        return Task.CompletedTask;
    }

    private void OnPlaybackStart(object? sender, MediaBrowser.Controller.Library.PlaybackProgressEventArgs e)
        => TryCapture(e, "start");

    private void OnPlaybackProgress(object? sender, MediaBrowser.Controller.Library.PlaybackProgressEventArgs e)
        => TryCapture(e, "progress");

    private void OnPlaybackStopped(object? sender, MediaBrowser.Controller.Library.PlaybackProgressEventArgs e)
        => TryCapture(e, "stopped");

    private void TryCapture(MediaBrowser.Controller.Library.PlaybackProgressEventArgs e, string reason)
    {
        var cfg = Plugin.Instance?.Configuration;
        if (cfg is null || !cfg.Enabled)
            return;

        PlaybackEventHelper.TryGetGuid(e, "UserId", out var userId);
        PlaybackEventHelper.TryGetGuid(e, "ItemId", out var itemId);

        if (cfg.AllowedUserIds.Length > 0 && !Array.Exists(cfg.AllowedUserIds, id => string.Equals(id, userId.ToString(), StringComparison.OrdinalIgnoreCase)))
            return;

        // Heuristic: only store if we have a meaningful position
        PlaybackEventHelper.TryGetLong(e, "PlaybackPositionTicks", out var positionTicks);
        var positionSeconds = positionTicks / 10_000_000;

        if (positionSeconds < cfg.ResumeThresholdSeconds)
            return;

        try
        {
            var containers = _resolver.ResolveContainersForPlayback(e);
            foreach (var container in containers)
            {
                var snapshot = _calculator.BuildSnapshot(container, e);
                _store.Upsert(snapshot);
            }

            if (cfg.DebugLogging)
            {
                _logger.LogInformation("[PlaylistNextUp] captured {Reason} user={UserId} item={ItemId} pos={Pos}s containers={Count}",
                    reason, userId, itemId, positionSeconds, containers.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlaylistNextUp] failed to capture playback ({Reason}).", reason);
        }
    }
}
