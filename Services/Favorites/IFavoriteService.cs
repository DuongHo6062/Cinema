using MovieStreaming.DTOs.Favorites;

namespace MovieStreaming.Services.Favorites;

public interface IFavoriteService
{
    Task<FavoriteMovieDto> AddFavoriteAsync(int movieId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFavoriteAsync(int movieId, CancellationToken cancellationToken = default);
    Task<FavoriteListResponseDto> GetFavoritesAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<FavoriteStatusDto> GetFavoriteStatusAsync(int movieId, CancellationToken cancellationToken = default);
}
