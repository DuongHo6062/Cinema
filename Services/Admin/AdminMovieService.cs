using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Common;

namespace MovieStreaming.Services.Admin;

public class AdminMovieService : IAdminMovieService
{
    private readonly ApplicationDbContext _context;

    public AdminMovieService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminMovieListItemDto>> GetMoviesAsync(
        string? search,
        int? genreId,
        int? year,
        string? status,
        string? sortBy,
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

        var query = _context.Movies
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(m => m.Title.ToLower().Contains(searchLower) ||
                                     (m.OriginalTitle != null && m.OriginalTitle.ToLower().Contains(searchLower)));
        }

        if (genreId.HasValue)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));
        }

        if (year.HasValue)
        {
            query = query.Where(m => m.ReleaseYear == year.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status != null && m.Status.ToLower() == status.Trim().ToLower());
        }

        query = sortBy?.ToLowerInvariant() switch
        {
            "oldest" => query.OrderBy(m => m.CreatedAt),
            "title" => query.OrderBy(m => m.Title),
            "rating" => query.OrderByDescending(m => m.Rating).ThenByDescending(m => m.CreatedAt),
            _ => query.OrderByDescending(m => m.CreatedAt)
        };

        var totalItems = await query.CountAsync(cancellationToken);

        var movies = await query
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.Episodes)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = movies.Select(m => new AdminMovieListItemDto
        {
            Id = m.Id,
            Title = m.Title,
            Slug = m.Slug,
            OriginalTitle = m.OriginalTitle,
            PosterUrl = m.PosterUrl,
            ReleaseYear = m.ReleaseYear,
            MovieType = m.MovieType,
            Status = m.Status,
            Quality = m.Quality,
            Rating = m.Rating,
            EpisodeCount = m.Episodes.Count,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            Genres = m.MovieGenres.Where(mg => mg.Genre != null).Select(mg => mg.Genre!.Name).ToList()
        }).ToList();

        return new PagedResult<AdminMovieListItemDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminMovieDetailDto> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movie = await _context.Movies
            .AsNoTracking()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.Episodes)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {id} not found.");
        }

        return MapToMovieDetailDto(movie);
    }

    public async Task<AdminMovieDetailDto> CreateMovieAsync(AdminCreateMovieRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Movie title is required.");
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(request.Title)
            : SlugHelper.GenerateSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Invalid slug generated for movie.");
        }

        var existingSlug = await _context.Movies.AnyAsync(m => m.Slug == slug, cancellationToken);
        if (existingSlug)
        {
            throw new ConflictException($"Movie with slug '{slug}' already exists.");
        }

        var movie = new Movie
        {
            Title = request.Title.Trim(),
            Slug = slug,
            OriginalTitle = request.OriginalTitle?.Trim(),
            Description = request.Description?.Trim(),
            PosterUrl = request.PosterUrl?.Trim(),
            ThumbnailUrl = request.ThumbnailUrl?.Trim(),
            ReleaseYear = request.ReleaseYear,
            Duration = request.Duration,
            Country = request.Country?.Trim(),
            Language = request.Language?.Trim(),
            MovieType = request.MovieType?.Trim(),
            Status = request.Status?.Trim() ?? "ongoing",
            Quality = request.Quality?.Trim(),
            Rating = request.Rating,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (request.GenreIds != null && request.GenreIds.Count > 0)
        {
            var validGenreIds = await _context.Genres
                .Where(g => request.GenreIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);

            foreach (var gid in validGenreIds)
            {
                movie.MovieGenres.Add(new MovieGenre { Movie = movie, GenreId = gid });
            }
        }

        await _context.Movies.AddAsync(movie, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetMovieByIdAsync(movie.Id, cancellationToken);
    }

    public async Task<AdminMovieDetailDto> UpdateMovieAsync(int id, AdminUpdateMovieRequest request, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ValidationException("Movie title is required.");
        }

        var movie = await _context.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {id} not found.");
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(request.Title)
            : SlugHelper.GenerateSlug(request.Slug);

        if (slug != movie.Slug)
        {
            var existingSlug = await _context.Movies.AnyAsync(m => m.Slug == slug && m.Id != id, cancellationToken);
            if (existingSlug)
            {
                throw new ConflictException($"Movie with slug '{slug}' already exists.");
            }
            movie.Slug = slug;
        }

        movie.Title = request.Title.Trim();
        movie.OriginalTitle = request.OriginalTitle?.Trim();
        movie.Description = request.Description?.Trim();
        movie.PosterUrl = request.PosterUrl?.Trim();
        movie.ThumbnailUrl = request.ThumbnailUrl?.Trim();
        movie.ReleaseYear = request.ReleaseYear;
        movie.Duration = request.Duration;
        movie.Country = request.Country?.Trim();
        movie.Language = request.Language?.Trim();
        movie.MovieType = request.MovieType?.Trim();
        movie.Status = request.Status?.Trim();
        movie.Quality = request.Quality?.Trim();
        movie.Rating = request.Rating;
        movie.UpdatedAt = DateTime.UtcNow;

        if (request.GenreIds != null)
        {
            movie.MovieGenres.Clear();
            var validGenreIds = await _context.Genres
                .Where(g => request.GenreIds.Contains(g.Id))
                .Select(g => g.Id)
                .ToListAsync(cancellationToken);

            foreach (var gid in validGenreIds)
            {
                movie.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = gid });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return await GetMovieByIdAsync(movie.Id, cancellationToken);
    }

    public async Task<bool> DeleteMovieAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movie = await _context.Movies
            .Include(m => m.Episodes)
                .ThenInclude(e => e.Sources)
            .Include(m => m.Episodes)
                .ThenInclude(e => e.WatchHistories)
            .Include(m => m.WatchHistories)
            .Include(m => m.MovieGenres)
            .Include(m => m.Favorites)
            .Include(m => m.MovieRatings)
            .Include(m => m.MovieComments)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {id} not found.");
        }

        // Explicitly remove child episodes to satisfy Restrict foreign key constraint
        foreach (var episode in movie.Episodes)
        {
            _context.EpisodeSources.RemoveRange(episode.Sources);
            _context.WatchHistories.RemoveRange(episode.WatchHistories);
        }
        _context.Episodes.RemoveRange(movie.Episodes);

        // Remove related associations explicitly to avoid severed required relationship exceptions
        _context.MovieGenres.RemoveRange(movie.MovieGenres);
        _context.Favorites.RemoveRange(movie.Favorites);
        _context.MovieRatings.RemoveRange(movie.MovieRatings);
        _context.MovieComments.RemoveRange(movie.MovieComments);
        _context.WatchHistories.RemoveRange(movie.WatchHistories);

        // Remove movie
        _context.Movies.Remove(movie);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static AdminMovieDetailDto MapToMovieDetailDto(Movie m)
    {
        return new AdminMovieDetailDto
        {
            Id = m.Id,
            Title = m.Title,
            Slug = m.Slug,
            OriginalTitle = m.OriginalTitle,
            Description = m.Description,
            PosterUrl = m.PosterUrl,
            ThumbnailUrl = m.ThumbnailUrl,
            ReleaseYear = m.ReleaseYear,
            Duration = m.Duration,
            Country = m.Country,
            Language = m.Language,
            MovieType = m.MovieType,
            Status = m.Status,
            Quality = m.Quality,
            Rating = m.Rating,
            ExternalId = m.ExternalId,
            ExternalSource = m.ExternalSource,
            CreatedAt = m.CreatedAt,
            UpdatedAt = m.UpdatedAt,
            EpisodeCount = m.Episodes?.Count ?? 0,
            Genres = m.MovieGenres?
                .Where(mg => mg.Genre != null)
                .Select(mg => new AdminGenreDto
                {
                    Id = mg.Genre!.Id,
                    Name = mg.Genre.Name,
                    Slug = mg.Genre.Slug,
                    Description = mg.Genre.Description
                }).ToList() ?? new List<AdminGenreDto>()
        };
    }
}
