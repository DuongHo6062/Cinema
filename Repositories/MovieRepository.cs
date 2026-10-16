using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Movies;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly ApplicationDbContext _context;

    public MovieRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Movie> Items, int TotalCount)> GetMoviesAsync(
        MovieQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Movies
            .AsNoTracking()
            .AsQueryable();

        // 1. Keyword search (Title, OriginalTitle, Slug)
        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var term = parameters.Search.Trim().ToLower();
            query = query.Where(m =>
                m.Title.ToLower().Contains(term) ||
                (m.OriginalTitle != null && m.OriginalTitle.ToLower().Contains(term)) ||
                m.Slug.ToLower().Contains(term));
        }

        // 2. Filter by Genre ID
        if (parameters.GenreId.HasValue && parameters.GenreId.Value > 0)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == parameters.GenreId.Value));
        }

        // 3. Filter by Genre Slug or Name
        if (!string.IsNullOrWhiteSpace(parameters.Genre))
        {
            var genreTerm = parameters.Genre.Trim().ToLower();
            query = query.Where(m => m.MovieGenres.Any(mg =>
                mg.Genre.Slug.ToLower() == genreTerm ||
                mg.Genre.Name.ToLower() == genreTerm));
        }

        // 4. Filter by Release Year
        if (parameters.Year.HasValue && parameters.Year.Value > 0)
        {
            query = query.Where(m => m.ReleaseYear == parameters.Year.Value);
        }

        // 5. Filter by Movie Type
        if (!string.IsNullOrWhiteSpace(parameters.Type))
        {
            var typeTerm = parameters.Type.Trim().ToLower();
            query = query.Where(m => m.MovieType != null && m.MovieType.ToLower() == typeTerm);
        }

        // 6. Filter by Country
        if (!string.IsNullOrWhiteSpace(parameters.Country))
        {
            var countryTerm = parameters.Country.Trim().ToLower();
            query = query.Where(m => m.Country != null && m.Country.ToLower().Contains(countryTerm));
        }

        // 7. Filter by Language
        if (!string.IsNullOrWhiteSpace(parameters.Language))
        {
            var langTerm = parameters.Language.Trim().ToLower();
            query = query.Where(m => m.Language != null && m.Language.ToLower().Contains(langTerm));
        }

        // 8. Filter by Original Language
        if (!string.IsNullOrWhiteSpace(parameters.OriginalLanguage))
        {
            var origLangTerm = parameters.OriginalLanguage.Trim().ToLower();
            query = query.Where(m => m.OriginalLanguage != null && m.OriginalLanguage.ToLower().Contains(origLangTerm));
        }

        // 9. Filter by Vietsub status
        if (parameters.IsVietsub.HasValue)
        {
            query = query.Where(m => m.IsVietsub == parameters.IsVietsub.Value);
        }

        // 10. Whitelisted Sorting
        var sortBy = parameters.SortBy?.Trim().ToLowerInvariant();
        query = sortBy switch
        {
            "oldest" => query.OrderBy(m => m.ReleaseYear).ThenBy(m => m.CreatedAt).ThenBy(m => m.Id),
            "rating" => query.OrderByDescending(m => (double?)m.Rating).ThenByDescending(m => m.CreatedAt).ThenBy(m => m.Id),
            "title" => query.OrderBy(m => m.Title).ThenBy(m => m.Id),
            _ => query.OrderByDescending(m => m.ReleaseYear).ThenByDescending(m => m.CreatedAt).ThenByDescending(m => m.Id)
        };

        // 7. Calculate Total Count before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // 8. Apply Pagination and Projection Inclusions
        var items = await query
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<(IEnumerable<Movie> Items, int TotalCount)> GetPagedMoviesAsync(int page, int pageSize)
    {
        return await GetMoviesAsync(new MovieQueryParameters { Page = page, PageSize = pageSize });
    }

    public async Task<Movie?> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Movies
            .AsNoTracking()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.Episodes.OrderBy(e => e.DisplayOrder.HasValue ? e.DisplayOrder.Value : e.EpisodeNumber).ThenBy(e => e.EpisodeNumber).ThenBy(e => e.Id))
                .ThenInclude(e => e.Sources.OrderBy(s => s.ServerName).ThenBy(s => s.Id))
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<Movie?> GetMovieBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLower();
        return await _context.Movies
            .AsNoTracking()
            .Include(m => m.MovieGenres)
                .ThenInclude(mg => mg.Genre)
            .Include(m => m.Episodes.OrderBy(e => e.DisplayOrder.HasValue ? e.DisplayOrder.Value : e.EpisodeNumber).ThenBy(e => e.EpisodeNumber).ThenBy(e => e.Id))
                .ThenInclude(e => e.Sources.OrderBy(s => s.ServerName).ThenBy(s => s.Id))
            .FirstOrDefaultAsync(m => m.Slug.ToLower() == normalizedSlug, cancellationToken);
    }
}
