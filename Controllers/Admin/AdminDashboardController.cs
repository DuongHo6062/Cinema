using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get high-level system statistics (counts of movies, users, genres, episodes, comments, etc.).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(AdminDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminDashboardDto>> GetStatistics(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetStatisticsAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get recent activity (newest users, movies, comments, and ratings).
    /// </summary>
    [HttpGet("recent")]
    [ProducesResponseType(typeof(AdminRecentActivityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AdminRecentActivityDto>> GetRecentActivity(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetRecentActivityAsync(cancellationToken);
        return Ok(result);
    }
}
