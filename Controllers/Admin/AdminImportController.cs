using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.Services.KKPhim;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/import")]
[Authorize(Roles = "Admin")]
public class AdminImportController : ControllerBase
{
    private readonly IKKPhimImportService _importService;
    private readonly ILogger<AdminImportController> _logger;

    public AdminImportController(IKKPhimImportService importService, ILogger<AdminImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger synchronization / import of a movie from KKPhim catalog by slug.
    /// </summary>
    [HttpPost("kkphim/movie/{slug}")]
    [ProducesResponseType(typeof(MovieImportResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieImportResultDto>> ImportMovieBySlug(
        [FromRoute] string slug,
        [FromQuery] bool vietsubOnly = true,
        [FromQuery] bool englishOnly = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin triggered KKPhim movie import for slug: {Slug} (VietsubOnly: {VietsubOnly}, EnglishOnly: {EnglishOnly})",
            slug, vietsubOnly, englishOnly);
        var result = await _importService.ImportMovieBySlugAsync(slug, vietsubOnly, englishOnly, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Admin bulk synchronization of movies from KKPhim catalog without downloading video files.
    /// </summary>
    [HttpPost("kkphim/sync")]
    [ProducesResponseType(typeof(KKPhimSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<KKPhimSyncResultDto>> SyncCatalog(
        [FromBody] KKPhimSyncRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin triggered KKPhim bulk sync: Pages {From}-{To}, Source: {Source}",
            request.FromPage, request.ToPage, request.SourceType);
        var result = await _importService.SyncCatalogAsync(request, cancellationToken);
        return Ok(result);
    }
}
