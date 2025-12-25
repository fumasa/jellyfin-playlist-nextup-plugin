using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Plugin.PlaylistNextUp.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaylistNextUp.Controllers;

/// <summary>
/// API endpoints for resume/next-up responses.
/// </summary>
[ApiController]
[Route("PlaylistNextUp")]
public class PlaylistNextUpController : ControllerBase
{
    private readonly ProgressStore _store;
    private readonly NextUpCalculator _calculator;
    private readonly IAuthorizationContext _authContext;
    private readonly ILogger<PlaylistNextUpController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaylistNextUpController"/> class.
    /// </summary>
    public PlaylistNextUpController(
        ProgressStore store,
        NextUpCalculator calculator,
        IAuthorizationContext authContext,
        ILogger<PlaylistNextUpController> logger)
    {
        _store = store;
        _calculator = calculator;
        _authContext = authContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets resume candidates for the current user.
    /// </summary>
    [HttpGet("Resume")]
    public async Task<ActionResult<IReadOnlyList<ResumeCandidate>>> Resume()
    {
        var resolvedUserId = await ResolveUserIdAsync();
        if (resolvedUserId == Guid.Empty)
            return Unauthorized();

        var snapshots = _store.GetAllForUser(resolvedUserId);
        var candidates = snapshots
            .Select(s => _calculator.ComputeResume(resolvedUserId, s))
            .Where(c => c != null)
            .Select(c => c!)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToList();

        return Ok(candidates);
    }

    /// <summary>
    /// Gets the resume candidate for a playlist.
    /// </summary>
    [HttpGet("Resume/playlist/{playlistId:guid}")]
    public async Task<ActionResult<ResumeCandidate>> ResumePlaylist([FromRoute] Guid playlistId)
    {
        var resolvedUserId = await ResolveUserIdAsync();
        if (resolvedUserId == Guid.Empty)
            return Unauthorized();

        if (!_store.TryGet(resolvedUserId, ContainerType.Playlist, playlistId, out var snap))
            return NotFound();

        var candidate = _calculator.ComputeResume(resolvedUserId, snap);
        return candidate is null ? NotFound() : Ok(candidate);
    }

    /// <summary>
    /// Gets the resume candidate for a collection.
    /// </summary>
    [HttpGet("Resume/collection/{collectionId:guid}")]
    public async Task<ActionResult<ResumeCandidate>> ResumeCollection([FromRoute] Guid collectionId)
    {
        var resolvedUserId = await ResolveUserIdAsync();
        if (resolvedUserId == Guid.Empty)
            return Unauthorized();

        if (!_store.TryGet(resolvedUserId, ContainerType.Collection, collectionId, out var snap))
            return NotFound();

        var candidate = _calculator.ComputeResume(resolvedUserId, snap);
        return candidate is null ? NotFound() : Ok(candidate);
    }

    private async Task<Guid> ResolveUserIdAsync()
    {
        try
        {
            var auth = await _authContext.GetAuthorizationInfo(HttpContext);
            if (auth?.UserId != null && auth.UserId != Guid.Empty)
                return auth.UserId;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[PlaylistNextUp] Failed to resolve user from token.");
        }

        return Guid.Empty;
    }
}
