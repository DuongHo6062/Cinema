using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Episodes;
using MovieStreaming.DTOs.Movies;
using MovieStreaming.DTOs.Recommendations;
using MovieStreaming.Services;
using MovieStreaming.Services.Recommendations;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IEpisodeService _episodeService;
    private readonly IRecommendationService _recommendationService;

    public MoviesController(
        IMovieService movieService,
        IEpisodeService episodeService)
        : this(movieService, episodeService, null!)
    {
    }

    [Microsoft.Extensions.DependencyInjection.ActivatorUtilitiesConstructor]
    public MoviesController(
        IMovieService movieService,
        IEpisodeService episodeService,
        IRecommendationService recommendationService)
    {
        _movieService = movieService;
        _episodeService = episodeService;
        _recommendationService = recommendationService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MovieListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovieListResponseDto>> GetMovies(
        [FromQuery] MovieQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _movieService.GetMoviesAsync(parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MovieDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieDetailDto>> GetMovieById(int id, CancellationToken cancellationToken)
    {
        var result = await _movieService.GetMovieByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(MovieDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovieDetailDto>> GetMovieBySlug(string slug, CancellationToken cancellationToken)
    {
        var result = await _movieService.GetMovieBySlugAsync(slug, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/episodes")]
    [ProducesResponseType(typeof(IReadOnlyList<EpisodeListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<EpisodeListItemDto>>> GetMovieEpisodes(
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetEpisodesByMovieIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/episodes/{episodeId:int}")]
    [ProducesResponseType(typeof(EpisodeDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EpisodeDetailDto>> GetMovieEpisodeById(
        int id,
        int episodeId,
        CancellationToken cancellationToken)
    {
        var result = await _episodeService.GetEpisodeByMovieAndIdAsync(id, episodeId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}/similar")]
    [ProducesResponseType(typeof(PagedResult<RecommendationMovieDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<RecommendationMovieDto>>> GetSimilarMovies(
        int id,
        [FromQuery] RecommendationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _recommendationService.GetSimilarMoviesAsync(id, parameters, cancellationToken);
        return Ok(result);
    }
}
