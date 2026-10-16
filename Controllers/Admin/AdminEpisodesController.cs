using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
public class AdminEpisodesController : ControllerBase
{
    private readonly IAdminEpisodeService _episodeService;

    public AdminEpisodesController(IAdminEpisodeService episodeService)
    {
        _episodeService = episodeService;
    }

    /// <summary>
    /// Get all episodes for a movie in deterministic order.
    /// </summary>
    [HttpGet("api/admin/movies/{movieId:int}/episodes")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminEpisodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AdminEpisodeDto>>> GetEpisodesByMovie(
        [FromRoute] int movieId,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetEpisodesByMovieIdAsync(movieId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Add an episode to a movie.
    /// </summary>
    [HttpPost("api/admin/movies/{movieId:int}/episodes")]
    [ProducesResponseType(typeof(AdminEpisodeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminEpisodeDto>> CreateEpisode(
        [FromRoute] int movieId,
        [FromBody] AdminCreateEpisodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.CreateEpisodeAsync(movieId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get episode details by ID.
    /// </summary>
    [HttpGet("api/admin/episodes/{episodeId:int}")]
    [ProducesResponseType(typeof(AdminEpisodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminEpisodeDto>> GetEpisodeById(
        [FromRoute] int episodeId,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetEpisodeByIdAsync(episodeId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Update an existing episode.
    /// </summary>
    [HttpPut("api/admin/episodes/{episodeId:int}")]
    [ProducesResponseType(typeof(AdminEpisodeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminEpisodeDto>> UpdateEpisode(
        [FromRoute] int episodeId,
        [FromBody] AdminUpdateEpisodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.UpdateEpisodeAsync(episodeId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete an episode safely.
    /// </summary>
    [HttpDelete("api/admin/episodes/{episodeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEpisode(
        [FromRoute] int episodeId,
        CancellationToken cancellationToken)
    {
        await _episodeService.DeleteEpisodeAsync(episodeId, cancellationToken);
        return Ok(new { success = true, message = $"Episode with ID {episodeId} deleted successfully." });
    }
}
