using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Ratings;
using MovieStreaming.Services.Ratings;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/movies/{movieId:int}")]
public class MovieRatingsController : ControllerBase
{
    private readonly IMovieRatingService _movieRatingService;

    public MovieRatingsController(IMovieRatingService movieRatingService)
    {
        _movieRatingService = movieRatingService;
    }

    /// <summary>
    /// Add or update current user's rating for a movie (1-10).
    /// </summary>
    [HttpPost("rating")]
    [Authorize]
    [ProducesResponseType(typeof(RatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RatingDto>> RateMovie(
        [FromRoute] int movieId,
        [FromBody] CreateRatingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieRatingService.RateMovieAsync(movieId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get current user's rating for a movie.
    /// </summary>
    [HttpGet("rating/me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserRatingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CurrentUserRatingDto>> GetCurrentUserRating(
        [FromRoute] int movieId,
        CancellationToken cancellationToken)
    {
        var result = await _movieRatingService.GetCurrentUserRatingAsync(movieId, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete current user's rating for a movie.
    /// </summary>
    [HttpDelete("rating")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRating(
        [FromRoute] int movieId,
        CancellationToken cancellationToken)
    {
        await _movieRatingService.DeleteRatingAsync(movieId, cancellationToken);
        return Ok(new { success = true, message = "Rating removed successfully." });
    }

    /// <summary>
    /// Get public rating summary and distribution for a movie.
    /// </summary>
    [HttpGet("rating-summary")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RatingSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RatingSummaryDto>> GetRatingSummary(
        [FromRoute] int movieId,
        CancellationToken cancellationToken)
    {
        var result = await _movieRatingService.GetRatingSummaryAsync(movieId, cancellationToken);
        return Ok(result);
    }
}
