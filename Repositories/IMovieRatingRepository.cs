using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IMovieRatingRepository
{
    Task<MovieRating?> GetByUserAndMovieIdAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    Task<MovieRating> AddAsync(MovieRating rating, CancellationToken cancellationToken = default);
    Task UpdateAsync(MovieRating rating, CancellationToken cancellationToken = default);
    Task<bool> DeleteByUserAndMovieIdAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    Task<(decimal AverageRating, int TotalRatings, Dictionary<int, int> Distribution)> GetRatingSummaryAsync(int movieId, CancellationToken cancellationToken = default);
}
