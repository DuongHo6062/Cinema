using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Comments;
using MovieStreaming.Services.Comments;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/movies/{movieId:int}/comments")]
public class MovieCommentsController : ControllerBase
{
    private readonly IMovieCommentService _movieCommentService;

    public MovieCommentsController(IMovieCommentService movieCommentService)
    {
        _movieCommentService = movieCommentService;
    }

    /// <summary>
    /// Add a new comment to a movie.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddComment(
        [FromRoute] int movieId,
        [FromBody] CreateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieCommentService.AddCommentAsync(movieId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Get paginated comments for a movie (newest first). Publicly accessible; shows isOwner if authenticated.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CommentListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentListResponseDto>> GetMovieComments(
        [FromRoute] int movieId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _movieCommentService.GetMovieCommentsAsync(movieId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
