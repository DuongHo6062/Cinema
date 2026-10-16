using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/movies")]
[Authorize(Roles = "Admin")]
public class AdminMoviesController : ControllerBase
{
    private readonly IAdminMovieService _movieService;

    public AdminMoviesController(IAdminMovieService movieService)
    {
        _movieService = movieService;
    }

    /// <summary>
    /// Get paginated movies for admin management with filtering and sorting.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminMovieListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminMovieListItemDto>>> GetMovies(
        [FromQuery] string? search,
        [FromQuery] int? genreId,
        [FromQuery] int? year,
        [FromQuery] string? status,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _movieService.GetMoviesAsync(search, genreId, year, status, sortBy, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get detailed movie information by ID for editing.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminMovieDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminMovieDetailDto>> GetMovieById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.GetMovieByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create a new movie with optional genres.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AdminMovieDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminMovieDetailDto>> CreateMovie(
        [FromBody] AdminCreateMovieRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.CreateMovieAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Update an existing movie.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AdminMovieDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminMovieDetailDto>> UpdateMovie(
        [FromRoute] int id,
        [FromBody] AdminUpdateMovieRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.UpdateMovieAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a movie safely along with its episodes, sources, and child records.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMovie(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _movieService.DeleteMovieAsync(id, cancellationToken);
        return Ok(new { success = true, message = $"Movie with ID {id} deleted successfully." });
    }
}
