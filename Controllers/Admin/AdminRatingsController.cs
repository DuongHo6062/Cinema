using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/ratings")]
[Authorize(Roles = "Admin")]
public class AdminRatingsController : ControllerBase
{
    private readonly IAdminModerationService _moderationService;

    public AdminRatingsController(IAdminModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    /// <summary>
    /// View paginated user ratings for monitoring.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminRatingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminRatingDto>>> GetRatings(
        [FromQuery] int? movieId,
        [FromQuery] int? userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _moderationService.GetRatingsAsync(movieId, userId, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
