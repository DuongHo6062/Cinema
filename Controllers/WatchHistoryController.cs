using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.WatchHistory;
using MovieStreaming.Services.WatchHistory;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/watch-history")]
[Authorize]
public class WatchHistoryController : ControllerBase
{
    private readonly IWatchHistoryService _watchHistoryService;

    public WatchHistoryController(IWatchHistoryService watchHistoryService)
    {
        _watchHistoryService = watchHistoryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(WatchHistoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WatchHistoryItemDto>> SaveProgress(
        [FromBody] SaveWatchHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _watchHistoryService.SaveWatchProgressAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(WatchHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WatchHistoryResponseDto>> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _watchHistoryService.GetWatchHistoryAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("continue")]
    [ProducesResponseType(typeof(IReadOnlyList<WatchHistoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<WatchHistoryItemDto>>> GetContinueWatching(
        [FromQuery] int limit = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _watchHistoryService.GetContinueWatchingAsync(limit, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{episodeId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHistoryItem(
        int episodeId,
        CancellationToken cancellationToken)
    {
        await _watchHistoryService.DeleteWatchHistoryItemAsync(episodeId, cancellationToken);
        return Ok(new { success = true, message = "Watch history item removed successfully." });
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ClearHistory(CancellationToken cancellationToken)
    {
        await _watchHistoryService.ClearWatchHistoryAsync(cancellationToken);
        return Ok(new { success = true, message = "Watch history cleared successfully." });
    }
}
