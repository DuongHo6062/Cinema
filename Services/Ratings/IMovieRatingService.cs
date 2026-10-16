using MovieStreaming.DTOs.Ratings;

namespace MovieStreaming.Services.Ratings;

public interface IMovieRatingService
{
    Task<RatingDto> RateMovieAsync(int movieId, CreateRatingRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserRatingDto> GetCurrentUserRatingAsync(int movieId, CancellationToken cancellationToken = default);
    Task<bool> DeleteRatingAsync(int movieId, CancellationToken cancellationToken = default);
    Task<RatingSummaryDto> GetRatingSummaryAsync(int movieId, CancellationToken cancellationToken = default);
}
