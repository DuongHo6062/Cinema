using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Favorites;
using MovieStreaming.Services.Favorites;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpPost("{movieId:int}")]
    [ProducesResponseType(typeof(FavoriteMovieDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FavoriteMovieDto>> AddFavorite(
        int movieId,
        CancellationToken cancellationToken)
    {
        var result = await _favoriteService.AddFavoriteAsync(movieId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{movieId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFavorite(
        int movieId,
        CancellationToken cancellationToken)
    {
        await _favoriteService.RemoveFavoriteAsync(movieId, cancellationToken);
        return Ok(new { success = true, message = "Movie removed from favorites successfully." });
    }

    [HttpGet]
    [ProducesResponseType(typeof(FavoriteListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteListResponseDto>> GetFavorites(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _favoriteService.GetFavoritesAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{movieId:int}/status")]
    [ProducesResponseType(typeof(FavoriteStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FavoriteStatusDto>> GetFavoriteStatus(
        int movieId,
        CancellationToken cancellationToken)
    {
        var result = await _favoriteService.GetFavoriteStatusAsync(movieId, cancellationToken);
        return Ok(result);
    }
}
