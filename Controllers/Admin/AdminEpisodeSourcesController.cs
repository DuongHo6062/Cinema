using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
public class AdminEpisodeSourcesController : ControllerBase
{
    private readonly IAdminEpisodeService _episodeService;

    public AdminEpisodeSourcesController(IAdminEpisodeService episodeService)
    {
        _episodeService = episodeService;
    }

    /// <summary>
    /// Get all video sources for an episode.
    /// </summary>
    [HttpGet("api/admin/episodes/{episodeId:int}/sources")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEpisodeSourceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdminEpisodeSourceDto>>> GetSourcesByEpisode(
        [FromRoute] int episodeId,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetSourcesByEpisodeIdAsync(episodeId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add a video source to an episode.
    /// </summary>
    [HttpPost("api/admin/episodes/{episodeId:int}/sources")]
    [ProducesResponseType(typeof(AdminEpisodeSourceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminEpisodeSourceDto>> CreateSource(
        [FromRoute] int episodeId,
        [FromBody] AdminCreateEpisodeSourceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.CreateSourceAsync(episodeId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update an existing video source.
    /// </summary>
    [HttpPut("api/admin/episode-sources/{sourceId:int}")]
    [ProducesResponseType(typeof(AdminEpisodeSourceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminEpisodeSourceDto>> UpdateSource(
        [FromRoute] int sourceId,
        [FromBody] AdminUpdateEpisodeSourceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.UpdateSourceAsync(sourceId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a video source.
    /// </summary>
    [HttpDelete("api/admin/episode-sources/{sourceId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSource(
        [FromRoute] int sourceId,
        CancellationToken cancellationToken)
    {
        await _episodeService.DeleteSourceAsync(sourceId, cancellationToken);
        return Ok(new { success = true, message = $"Episode source with ID {sourceId} deleted successfully." });
    }
}
