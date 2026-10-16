using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovieStreaming.Configurations;
using MovieStreaming.Data;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Recommendations;
using MovieStreaming.Exceptions;

namespace MovieStreaming.Services.Recommendations;

public class RecommendationService : IRecommendationService
{
    private readonly ApplicationDbContext _context;
    private readonly IRecommendationScorer _scorer;
    private readonly RecommendationOptions _options;

    public RecommendationService(
        ApplicationDbContext context,
        IRecommendationScorer scorer,
        IOptions<RecommendationOptions> options)
    {
        _context = context;
        _scorer = scorer;
        _options = options.Value ?? new RecommendationOptions();
    }

    public async Task<PagedResult<RecommendationMovieDto>> GetSimilarMoviesAsync(
        int movieId,
        RecommendationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(parameters);

        var target = await _context.Movies
            .AsNoTracking()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.Id == movieId, cancellationToken);

        if (target == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var targetGenreIds = target.MovieGenres.Select(mg => mg.GenreId).ToHashSet();
        var targetGenreNames = target.MovieGenres.Select(mg => mg.Genre.Name).ToHashSet();

        // Retrieve candidate movies excluding the target movie itself
        var candidateQuery = _context.Movies
            .AsNoTracking()
            .Where(m => m.Id != movieId);

        var candidates = await candidateQuery
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Slug,
                m.OriginalTitle,
                m.PosterUrl,
                m.ThumbnailUrl,
                m.ReleaseYear,
                m.Rating,
                m.MovieType,
                GenreIds = m.MovieGenres.Select(mg => mg.GenreId).ToList(),
                GenreNames = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
            })
            .Take(300)
            .ToListAsync(cancellationToken);

        var scored = candidates.Select(c =>
        {
            var score = _scorer.CalculateSimilarScore(
                targetGenreIds,
                c.GenreIds,
                target.MovieType,
                c.MovieType,
                target.ReleaseYear,
                c.ReleaseYear,
                c.Rating);

            var sharedGenres = targetGenreNames.Intersect(c.GenreNames).ToList();
            string reason;
            if (sharedGenres.Count > 0)
            {
                reason = $"Similar genres: {string.Join(", ", sharedGenres)}";
            }
            else if (!string.IsNullOrWhiteSpace(target.MovieType) &&
                     string.Equals(target.MovieType, c.MovieType, StringComparison.OrdinalIgnoreCase))
            {
                reason = $"Similar format: {c.MovieType}";
            }
            else
            {
                reason = "Similar release era and popularity";
            }

            return new RecommendationMovieDto
            {
                MovieId = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                OriginalTitle = c.OriginalTitle,
                PosterUrl = c.PosterUrl,
                ThumbnailUrl = c.ThumbnailUrl,
                ReleaseYear = c.ReleaseYear,
                Rating = c.Rating,
                MovieType = c.MovieType,
                Genres = c.GenreNames,
                Score = score,
                Reason = reason
            };
        })
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.Rating)
        .ThenByDescending(x => x.ReleaseYear)
        .ThenBy(x => x.MovieId)
        .ToList();

        var totalItems = scored.Count;
        var pagedItems = scored
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();

        return new PagedResult<RecommendationMovieDto>
        {
            Items = pagedItems,
            TotalItems = totalItems,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<PagedResult<RecommendationMovieDto>> GetPersonalizedRecommendationsAsync(
        int userId,
        RecommendationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(parameters);

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId, cancellationToken);

        if (!userExists)
        {
            throw new NotFoundException($"User with ID {userId} not found.");
        }

        // 1. Gather Watch History
        var watchHistories = await _context.WatchHistories
            .AsNoTracking()
            .Where(wh => wh.UserId == userId)
            .Select(wh => new
            {
                wh.MovieId,
                wh.LastWatchedAt,
                GenreIds = wh.Movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                GenreNames = wh.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                wh.Movie.MovieType
            })
            .ToListAsync(cancellationToken);

        // 2. Gather Favorites
        var favorites = await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => new
            {
                f.MovieId,
                f.CreatedAt,
                GenreIds = f.Movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                GenreNames = f.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                f.Movie.MovieType
            })
            .ToListAsync(cancellationToken);

        // 3. Gather Ratings
        var ratings = await _context.MovieRatings
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new
            {
                r.MovieId,
                r.Rating,
                GenreIds = r.Movie.MovieGenres.Select(mg => mg.GenreId).ToList(),
                GenreNames = r.Movie.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                r.Movie.MovieType
            })
            .ToListAsync(cancellationToken);

        // Record consumed movies for exclusion
        var consumedMovieIds = new HashSet<int>();
        foreach (var w in watchHistories) consumedMovieIds.Add(w.MovieId);
        foreach (var f in favorites) consumedMovieIds.Add(f.MovieId);
        foreach (var r in ratings) consumedMovieIds.Add(r.MovieId);

        // Cold-start fallback: user has no activity
        if (watchHistories.Count == 0 && favorites.Count == 0 && ratings.Count == 0)
        {
            return await GetPublicRecommendationsAsync(parameters, cancellationToken);
        }

        // Aggregate User Preferences
        var genrePreferences = new Dictionary<int, double>();
        var genreIdToName = new Dictionary<int, string>();
        var typePreferences = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;

        // Process watch history with recency decay
        foreach (var wh in watchHistories)
        {
            var daysAgo = Math.Max(0, (now - wh.LastWatchedAt).TotalDays);
            var recencyFactor = Math.Max(0.4, 1.0 - (daysAgo / 60.0));
            var weight = _options.WatchHistoryWeight * recencyFactor;

            for (int i = 0; i < wh.GenreIds.Count; i++)
            {
                var gId = wh.GenreIds[i];
                genrePreferences[gId] = genrePreferences.GetValueOrDefault(gId, 0.0) + weight;
                if (!genreIdToName.ContainsKey(gId) && i < wh.GenreNames.Count)
                    genreIdToName[gId] = wh.GenreNames[i];
            }

            if (!string.IsNullOrWhiteSpace(wh.MovieType))
                typePreferences[wh.MovieType] = typePreferences.GetValueOrDefault(wh.MovieType, 0.0) + weight;
        }

        // Process favorites (strong preference signal)
        foreach (var fav in favorites)
        {
            var weight = _options.FavoriteWeight;
            for (int i = 0; i < fav.GenreIds.Count; i++)
            {
                var gId = fav.GenreIds[i];
                genrePreferences[gId] = genrePreferences.GetValueOrDefault(gId, 0.0) + weight;
                if (!genreIdToName.ContainsKey(gId) && i < fav.GenreNames.Count)
                    genreIdToName[gId] = fav.GenreNames[i];
            }

            if (!string.IsNullOrWhiteSpace(fav.MovieType))
                typePreferences[fav.MovieType] = typePreferences.GetValueOrDefault(fav.MovieType, 0.0) + weight;
        }

        // Process ratings (positive / neutral / negative signals)
        foreach (var r in ratings)
        {
            double weight;
            if (r.Rating >= 8) weight = _options.RatingHighWeight;
            else if (r.Rating >= 6) weight = _options.RatingMediumWeight;
            else if (r.Rating <= 4) weight = _options.RatingLowWeight;
            else weight = 0.0;

            if (Math.Abs(weight) > 0.001)
            {
                for (int i = 0; i < r.GenreIds.Count; i++)
                {
                    var gId = r.GenreIds[i];
                    genrePreferences[gId] = genrePreferences.GetValueOrDefault(gId, 0.0) + weight;
                    if (!genreIdToName.ContainsKey(gId) && i < r.GenreNames.Count)
                        genreIdToName[gId] = r.GenreNames[i];
                }

                if (!string.IsNullOrWhiteSpace(r.MovieType))
                    typePreferences[r.MovieType] = typePreferences.GetValueOrDefault(r.MovieType, 0.0) + weight;
            }
        }

        var maxGenrePref = genrePreferences.Values.Where(v => v > 0).DefaultIfEmpty(1.0).Max();
        var topGenres = genrePreferences
            .Where(kv => kv.Value > 0)
            .OrderByDescending(kv => kv.Value)
            .Take(3)
            .Select(kv => genreIdToName.GetValueOrDefault(kv.Key, "movies"))
            .ToList();

        // Candidate pool
        var candidateQuery = _context.Movies.AsNoTracking();
        if (parameters.ExcludeConsumed && consumedMovieIds.Count > 0)
        {
            candidateQuery = candidateQuery.Where(m => !consumedMovieIds.Contains(m.Id));
        }

        var candidates = await candidateQuery
            .Select(m => new
            {
                m.Id,
                m.Title,
                m.Slug,
                m.OriginalTitle,
                m.PosterUrl,
                m.ThumbnailUrl,
                m.ReleaseYear,
                m.Rating,
                m.MovieType,
                GenreIds = m.MovieGenres.Select(mg => mg.GenreId).ToList(),
                GenreNames = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
            })
            .Take(300)
            .ToListAsync(cancellationToken);

        // If candidates are empty because all movies were consumed and excluded, fallback to all movies
        if (candidates.Count == 0 && parameters.ExcludeConsumed)
        {
            candidates = await _context.Movies
                .AsNoTracking()
                .Select(m => new
                {
                    m.Id,
                    m.Title,
                    m.Slug,
                    m.OriginalTitle,
                    m.PosterUrl,
                    m.ThumbnailUrl,
                    m.ReleaseYear,
                    m.Rating,
                    m.MovieType,
                    GenreIds = m.MovieGenres.Select(mg => mg.GenreId).ToList(),
                    GenreNames = m.MovieGenres.Select(mg => mg.Genre.Name).ToList()
                })
                .Take(100)
                .ToListAsync(cancellationToken);
        }

        var scored = candidates.Select(c =>
        {
            var score = _scorer.CalculatePersonalizedScore(
                c.GenreIds,
                c.MovieType,
                c.ReleaseYear,
                c.Rating,
                genrePreferences,
                typePreferences,
                maxGenrePref);

            var matchedTopGenres = c.GenreNames.Intersect(topGenres).ToList();
            string reason;
            if (matchedTopGenres.Count > 0)
            {
                reason = $"Because you enjoy {string.Join(" & ", matchedTopGenres)}";
            }
            else if (topGenres.Count > 0)
            {
                reason = $"Recommended based on your interest in {string.Join(", ", topGenres)}";
            }
            else
            {
                reason = "Popular choice based on overall ratings";
            }

            return new RecommendationMovieDto
            {
                MovieId = c.Id,
                Title = c.Title,
                Slug = c.Slug,
                OriginalTitle = c.OriginalTitle,
                PosterUrl = c.PosterUrl,
                ThumbnailUrl = c.ThumbnailUrl,
                ReleaseYear = c.ReleaseYear,
                Rating = c.Rating,
                MovieType = c.MovieType,
                Genres = c.GenreNames,
                Score = score,
                Reason = reason
            };
        })
        .OrderByDescending(x => x.Score)
        .ThenByDescending(x => x.Rating)
        .ThenByDescending(x => x.ReleaseYear)
        .ThenBy(x => x.MovieId)
        .ToList();

        var totalItems = scored.Count;
        var pagedItems = scored
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();

        return new PagedResult<RecommendationMovieDto>
        {
            Items = pagedItems,
            TotalItems = totalItems,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<PagedResult<RecommendationMovieDto>> GetPublicRecommendationsAsync(
        RecommendationQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(parameters);

        var query = _context.Movies
            .AsNoTracking()
            .OrderByDescending(m => (double?)m.Rating)
            .ThenByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id);

        var totalItems = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(m => new RecommendationMovieDto
            {
                MovieId = m.Id,
                Title = m.Title,
                Slug = m.Slug,
                OriginalTitle = m.OriginalTitle,
                PosterUrl = m.PosterUrl,
                ThumbnailUrl = m.ThumbnailUrl,
                ReleaseYear = m.ReleaseYear,
                Rating = m.Rating,
                MovieType = m.MovieType,
                Genres = m.MovieGenres.Select(mg => mg.Genre.Name).ToList(),
                Score = m.Rating.HasValue ? Math.Round((double)m.Rating.Value / 10.0, 4) : 0.5,
                Reason = "Popular and top-rated on SHIN movie"
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<RecommendationMovieDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    private static void ValidatePagination(RecommendationQueryParameters parameters)
    {
        if (parameters.Page < 1)
        {
            throw new ValidationException("Page must be greater than or equal to 1.");
        }

        if (parameters.PageSize > 100)
        {
            throw new ValidationException("PageSize cannot exceed 100.");
        }

        if (parameters.PageSize < 1)
        {
            parameters.PageSize = 20;
        }
    }
}
