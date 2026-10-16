using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Recommendations;

namespace MovieStreaming.Services.Recommendations;

public interface IRecommendationService
{
    Task<PagedResult<RecommendationMovieDto>> GetSimilarMoviesAsync(
        int movieId,
        RecommendationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RecommendationMovieDto>> GetPersonalizedRecommendationsAsync(
        int userId,
        RecommendationQueryParameters parameters,
        CancellationToken cancellationToken = default);

    Task<PagedResult<RecommendationMovieDto>> GetPublicRecommendationsAsync(
        RecommendationQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
