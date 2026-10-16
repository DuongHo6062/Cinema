using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IFavoriteRepository
{
    Task<Favorite?> GetByUserAndMovieIdAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    Task<bool> IsFavoriteAsync(int userId, int movieId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Favorite> Items, int TotalCount)> GetUserFavoritesAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Favorite> AddAsync(Favorite favorite, CancellationToken cancellationToken = default);
    Task<bool> DeleteByUserAndMovieIdAsync(int userId, int movieId, CancellationToken cancellationToken = default);
}
