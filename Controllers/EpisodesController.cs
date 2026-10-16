using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Episodes;
using MovieStreaming.Services;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EpisodesController : ControllerBase
{
    private readonly IEpisodeService _episodeService;

    public EpisodesController(IEpisodeService episodeService)
    {
        _episodeService = episodeService;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EpisodeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EpisodeDetailDto>> GetEpisodeById(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetEpisodeByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/sources")]
    [ProducesResponseType(typeof(EpisodeSourcesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EpisodeSourcesResponseDto>> GetEpisodeSources(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetEpisodeSourcesAsync(id, cancellationToken);
        return Ok(result);
    }
}
