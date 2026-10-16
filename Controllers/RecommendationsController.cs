using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Recommendations;
using MovieStreaming.Exceptions;
using MovieStreaming.Services.Authentication;
using MovieStreaming.Services.Recommendations;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;
    private readonly ICurrentUserService _currentUserService;

    public RecommendationsController(
        IRecommendationService recommendationService,
        ICurrentUserService currentUserService)
    {
        _recommendationService = recommendationService;
        _currentUserService = currentUserService;
    }

    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RecommendationMovieDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<RecommendationMovieDto>>> GetPersonalizedRecommendations(
        [FromQuery] RecommendationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var userId = _currentUserService.UserId;

        var result = await _recommendationService.GetPersonalizedRecommendationsAsync(userId, parameters, cancellationToken);
        return Ok(result);
    }

    [HttpGet("public")]
    [ProducesResponseType(typeof(PagedResult<RecommendationMovieDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<RecommendationMovieDto>>> GetPublicRecommendations(
        [FromQuery] RecommendationQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _recommendationService.GetPublicRecommendationsAsync(parameters, cancellationToken);
        return Ok(result);
    }
}
