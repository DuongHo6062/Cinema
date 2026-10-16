using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.Services.KKPhim;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImportController : ControllerBase
{
    private readonly IKKPhimImportService _importService;
    private readonly ILogger<ImportController> _logger;

    public ImportController(IKKPhimImportService importService, ILogger<ImportController> logger)
    {
        _importService = importService;
        _logger = logger;
    }

    /// <summary>
    /// Imports or synchronizes a movie from KKPhim into the local SQL Server database with Vietsub/English filtering.
    /// </summary>
    [HttpPost("kkphim/movie/{slug}")]
    public async Task<ActionResult<MovieImportResultDto>> ImportMovie(
        string slug,
        [FromQuery] bool vietsubOnly = true,
        [FromQuery] bool englishOnly = false,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Import request received for KKPhim slug: {Slug} (VietsubOnly: {VietsubOnly}, EnglishOnly: {EnglishOnly})",
            slug, vietsubOnly, englishOnly);
        var result = await _importService.ImportMovieBySlugAsync(slug, vietsubOnly, englishOnly, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Bulk synchronizes and imports movies from KKPhim catalog into SQL Server without downloading video files.
    /// Filters English-language movies and Vietsub-only subtitles by default.
    /// </summary>
    [HttpPost("kkphim/sync")]
    public async Task<ActionResult<KKPhimSyncResultDto>> SyncCatalog(
        [FromBody] KKPhimSyncRequestDto request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bulk sync requested: Pages {From}-{To}, Source: {Source}",
            request.FromPage, request.ToPage, request.SourceType);
        var result = await _importService.SyncCatalogAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Safe development endpoint: Previews KKPhim movie details without writing to the database.
    /// </summary>
    [HttpGet("kkphim/test/{slug}")]
    public async Task<ActionResult<KKPhimMoviePreviewDto>> TestMovieDetail(string slug, CancellationToken cancellationToken)
    {
        var preview = await _importService.GetMoviePreviewAsync(slug, cancellationToken);
        if (preview == null)
        {
            return NotFound(new ErrorResponse(404, $"Movie '{slug}' was not found on KKPhim."));
        }
        return Ok(preview);
    }

    /// <summary>
    /// Searches KKPhim catalog for movies matching a keyword.
    /// </summary>
    [HttpGet("kkphim/search")]
    public async Task<ActionResult<KKPhimSearchResultDto>> Search(
        [FromQuery] string keyword,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _importService.SearchMoviesAsync(keyword, page, cancellationToken);
        return Ok(result);
    }
}
