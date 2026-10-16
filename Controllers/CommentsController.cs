using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Comments;
using MovieStreaming.Services.Comments;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/comments/{commentId:int}")]
public class CommentsController : ControllerBase
{
    private readonly IMovieCommentService _movieCommentService;

    public CommentsController(IMovieCommentService movieCommentService)
    {
        _movieCommentService = movieCommentService;
    }

    /// <summary>
    /// Edit an existing comment. Only the comment author can edit it.
    /// </summary>
    [HttpPut]
    [Authorize]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> EditComment(
        [FromRoute] int commentId,
        [FromBody] UpdateCommentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _movieCommentService.EditCommentAsync(commentId, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete a comment. Only the comment author can delete it.
    /// </summary>
    [HttpDelete]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] int commentId,
        CancellationToken cancellationToken)
    {
        await _movieCommentService.DeleteCommentAsync(commentId, cancellationToken);
        return Ok(new { success = true, message = "Comment deleted successfully." });
    }
}
