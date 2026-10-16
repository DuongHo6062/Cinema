using MovieStreaming.DTOs.Favorites;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Services.Favorites;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ICurrentUserService _currentUserService;

    public FavoriteService(
        IFavoriteRepository favoriteRepository,
        IMovieRepository movieRepository,
        ICurrentUserService currentUserService)
    {
        _favoriteRepository = favoriteRepository;
        _movieRepository = movieRepository;
        _currentUserService = currentUserService;
    }

    public async Task<FavoriteMovieDto> AddFavoriteAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var userId = _currentUserService.UserId;

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var existing = await _favoriteRepository.GetByUserAndMovieIdAsync(userId, movieId, cancellationToken);
        if (existing != null)
        {
            // Already favorited — return existing cleanly without duplicate
            return MapToFavoriteMovieDto(existing, movie);
        }

        var favorite = new Favorite
        {
            UserId = userId,
            MovieId = movieId,
            CreatedAt = DateTime.UtcNow
        };

        await _favoriteRepository.AddAsync(favorite, cancellationToken);

        return MapToFavoriteMovieDto(favorite, movie);
    }

    public async Task<bool> RemoveFavoriteAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var userId = _currentUserService.UserId;
        var deleted = await _favoriteRepository.DeleteByUserAndMovieIdAsync(userId, movieId, cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException($"Favorite record for movie ID {movieId} not found.");
        }

        return true;
    }

    public async Task<FavoriteListResponseDto> GetFavoritesAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var userId = _currentUserService.UserId;
        var (items, totalCount) = await _favoriteRepository.GetUserFavoritesAsync(userId, page, pageSize, cancellationToken);

        return new FavoriteListResponseDto
        {
            Items = items.Select(f => MapToFavoriteMovieDto(f)).ToList(),
            TotalItems = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<FavoriteStatusDto> GetFavoriteStatusAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var userId = _currentUserService.UserId;
        var isFavorite = await _favoriteRepository.IsFavoriteAsync(userId, movieId, cancellationToken);

        return new FavoriteStatusDto
        {
            MovieId = movieId,
            IsFavorite = isFavorite
        };
    }

    private static FavoriteMovieDto MapToFavoriteMovieDto(Favorite f, Movie? fallbackMovie = null)
    {
        var m = f.Movie ?? fallbackMovie;
        return new FavoriteMovieDto
        {
            Id = f.MovieId,
            MovieId = f.MovieId,
            Title = m?.Title ?? string.Empty,
            Slug = m?.Slug ?? string.Empty,
            PosterUrl = m?.PosterUrl,
            ThumbnailUrl = m?.ThumbnailUrl,
            ReleaseYear = m?.ReleaseYear,
            MovieType = m?.MovieType,
            Quality = m?.Quality,
            Rating = m?.Rating,
            FavoritedAt = f.CreatedAt,
            CreatedAt = f.CreatedAt
        };
    }
}
