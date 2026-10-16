using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/genres")]
[Authorize(Roles = "Admin")]
public class AdminGenresController : ControllerBase
{
    private readonly IAdminGenreService _genreService;

    public AdminGenresController(IAdminGenreService genreService)
    {
        _genreService = genreService;
    }

    /// <summary>
    /// Get all genres for admin management.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AdminGenreDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<AdminGenreDto>>> GetAllGenres(CancellationToken cancellationToken)
    {
        var result = await _genreService.GetAllGenresAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new genre.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminGenreDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminGenreDto>> CreateGenre(
        [FromBody] AdminCreateGenreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _genreService.CreateGenreAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update an existing genre.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AdminGenreDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminGenreDto>> UpdateGenre(
        [FromRoute] int id,
        [FromBody] AdminUpdateGenreRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _genreService.UpdateGenreAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a genre.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGenre(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _genreService.DeleteGenreAsync(id, cancellationToken);
        return Ok(new { success = true, message = $"Genre with ID {id} deleted successfully." });
    }
}
