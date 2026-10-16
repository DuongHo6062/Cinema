using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/comments")]
[Authorize(Roles = "Admin")]
public class AdminCommentsController : ControllerBase
{
    private readonly IAdminModerationService _moderationService;

    public AdminCommentsController(IAdminModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    /// <summary>
    /// Get paginated comments across the platform with filtering for moderation.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminCommentDto>>> GetComments(
        [FromQuery] int? movieId,
        [FromQuery] int? userId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderationService.GetCommentsAsync(movieId, userId, search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get details of a single comment for moderation.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminCommentDto>> GetCommentById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _moderationService.GetCommentByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Delete an inappropriate or spam comment (Admin moderation).
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteComment(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        await _moderationService.DeleteCommentAsync(id, cancellationToken);
        return Ok(new { success = true, message = $"Comment with ID {id} removed by administrator." });
    }
}
