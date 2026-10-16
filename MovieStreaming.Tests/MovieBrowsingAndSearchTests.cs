using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Movies;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services;

namespace MovieStreaming.Tests;

public class MovieBrowsingAndSearchTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly MovieRepository _movieRepository;
    private readonly MovieService _movieService;
    private readonly GenreRepository _genreRepository;
    private readonly GenreService _genreService;

    private class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.GetColumnType() == "nvarchar(max)")
                    {
                        property.SetColumnType("TEXT");
                    }
                }
                foreach (var index in entity.GetIndexes())
                {
                    index.SetFilter(null);
                }
            }
        }
    }

    public MovieBrowsingAndSearchTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestApplicationDbContext(options);
        _context.Database.EnsureCreated();

        SeedTestData();

        _movieRepository = new MovieRepository(_context);
        _movieService = new MovieService(_movieRepository);

        _genreRepository = new GenreRepository(_context);
        _genreService = new GenreService(_genreRepository);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private void SeedTestData()
    {
        // Clean seed data to have deterministic test records
        _context.EpisodeSources.RemoveRange(_context.EpisodeSources);
        _context.Episodes.RemoveRange(_context.Episodes);
        _context.MovieGenres.RemoveRange(_context.MovieGenres);
        _context.Movies.RemoveRange(_context.Movies);
        _context.Genres.RemoveRange(_context.Genres);
        _context.SaveChanges();

        var actionGenre = new Genre { Id = 1, Name = "Action", Slug = "action" };
        var dramaGenre = new Genre { Id = 2, Name = "Drama", Slug = "drama" };
        var sciFiGenre = new Genre { Id = 3, Name = "Sci-Fi", Slug = "sci-fi" };
        var animeGenre = new Genre { Id = 4, Name = "Animation", Slug = "animation" };

        _context.Genres.AddRange(actionGenre, dramaGenre, sciFiGenre, animeGenre);
        _context.SaveChanges();

        var movies = new List<Movie>
        {
            new()
            {
                Id = 1,
                Title = "Naruto Shippuden",
                OriginalTitle = "Naruto: Shippūden",
                Slug = "naruto-shippuden",
                ReleaseYear = 2007,
                MovieType = "series",
                Rating = 8.7m,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                MovieGenres = new List<MovieGenre>
                {
                    new() { GenreId = 1 },
                    new() { GenreId = 4 }
                }
            },
            new()
            {
                Id = 2,
                Title = "One Piece",
                OriginalTitle = "Wan Pīsu",
                Slug = "one-piece",
                ReleaseYear = 1999,
                MovieType = "series",
                Rating = 9.0m,
                CreatedAt = new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                MovieGenres = new List<MovieGenre>
                {
                    new() { GenreId = 1 },
                    new() { GenreId = 4 }
                }
            },
            new()
            {
                Id = 3,
                Title = "Inception",
                OriginalTitle = "Inception",
                Slug = "inception",
                ReleaseYear = 2010,
                MovieType = "single",
                Rating = 8.8m,
                CreatedAt = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                MovieGenres = new List<MovieGenre>
                {
                    new() { GenreId = 1 },
                    new() { GenreId = 3 }
                }
            },
            new()
            {
                Id = 4,
                Title = "Interstellar",
                OriginalTitle = "Interstellar",
                Slug = "interstellar",
                ReleaseYear = 2014,
                MovieType = "single",
                Rating = 8.6m,
                CreatedAt = new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                MovieGenres = new List<MovieGenre>
                {
                    new() { GenreId = 2 },
                    new() { GenreId = 3 }
                }
            },
            new()
            {
                Id = 5,
                Title = "The Godfather",
                OriginalTitle = "The Godfather",
                Slug = "the-godfather",
                ReleaseYear = 1972,
                MovieType = "single",
                Rating = 9.2m,
                CreatedAt = new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                MovieGenres = new List<MovieGenre>
                {
                    new() { GenreId = 2 }
                }
            }
        };

        _context.Movies.AddRange(movies);
        _context.SaveChanges();
    }

    // 1. Get first movie page
    [Fact]
    public async Task GetMoviesAsync_FirstPage_ReturnsFirstPageWithExpectedCount()
    {
        var parameters = new MovieQueryParameters { Page = 1, PageSize = 3 };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.NotNull(result);
        Assert.Equal(1, result.Page);
        Assert.Equal(3, result.PageSize);
        Assert.Equal(5, result.TotalItems);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(3, result.Items.Count());
    }

    // 2. Pagination (second page)
    [Fact]
    public async Task GetMoviesAsync_SecondPage_ReturnsRemainingItems()
    {
        var parameters = new MovieQueryParameters { Page = 2, PageSize = 3 };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.Items.Count());
        Assert.Equal(5, result.TotalItems);
    }

    // 3. Search by title
    [Fact]
    public async Task GetMoviesAsync_SearchByTitle_ReturnsMatchingMovies()
    {
        var parameters = new MovieQueryParameters { Search = "naruto" };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Single(result.Items);
        Assert.Equal("Naruto Shippuden", result.Items.First().Title);
    }

    // 4. Search by original title
    [Fact]
    public async Task GetMoviesAsync_SearchByOriginalTitle_ReturnsMatchingMovies()
    {
        var parameters = new MovieQueryParameters { Search = "Wan Pīsu" };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Single(result.Items);
        Assert.Equal("One Piece", result.Items.First().Title);
    }

    // 5. Filter by genre (ID and Slug)
    [Fact]
    public async Task GetMoviesAsync_FilterByGenreId_ReturnsMoviesWithThatGenre()
    {
        // GenreId = 3 is Sci-Fi (Inception and Interstellar)
        var parameters = new MovieQueryParameters { GenreId = 3 };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, m => Assert.Contains(m.Genres, g => g.Id == 3));
    }

    [Fact]
    public async Task GetMoviesAsync_FilterByGenreSlug_ReturnsMoviesWithThatGenre()
    {
        // Genre slug = "drama" (Interstellar and The Godfather)
        var parameters = new MovieQueryParameters { Genre = "drama" };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, m => Assert.Contains(m.Genres, g => g.Slug == "drama"));
    }

    // 6. Filter by year
    [Fact]
    public async Task GetMoviesAsync_FilterByYear_ReturnsOnlyMoviesFromThatYear()
    {
        var parameters = new MovieQueryParameters { Year = 2010 };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Single(result.Items);
        Assert.Equal("Inception", result.Items.First().Title);
    }

    // 7. Filter by movie type/category
    [Fact]
    public async Task GetMoviesAsync_FilterByMovieType_ReturnsMatchingTypes()
    {
        var parameters = new MovieQueryParameters { Type = "series" };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Equal(2, result.Items.Count());
        Assert.All(result.Items, m => Assert.Equal("series", m.MovieType));
    }

    // 8. Sort by newest
    [Fact]
    public async Task GetMoviesAsync_SortByNewest_OrdersByReleaseYearDescending()
    {
        var parameters = new MovieQueryParameters { SortBy = "newest" };
        var result = await _movieService.GetMoviesAsync(parameters);

        var years = result.Items.Select(m => m.ReleaseYear).ToList();
        Assert.Equal(years.OrderByDescending(y => y).ToList(), years);
    }

    // 9. Sort by rating
    [Fact]
    public async Task GetMoviesAsync_SortByRating_OrdersByRatingDescending()
    {
        var parameters = new MovieQueryParameters { SortBy = "rating" };
        var result = await _movieService.GetMoviesAsync(parameters);

        var ratings = result.Items.Select(m => m.Rating).ToList();
        Assert.Equal(ratings.OrderByDescending(r => r).ToList(), ratings);
        Assert.Equal("The Godfather", result.Items.First().Title); // Rating = 9.2
    }

    // 10. Sort by title
    [Fact]
    public async Task GetMoviesAsync_SortByTitle_OrdersAlphabetically()
    {
        var parameters = new MovieQueryParameters { SortBy = "title" };
        var result = await _movieService.GetMoviesAsync(parameters);

        var titles = result.Items.Select(m => m.Title).ToList();
        Assert.Equal(titles.OrderBy(t => t).ToList(), titles);
        Assert.Equal("Inception", result.Items.First().Title);
    }

    // 11. Combine multiple filters
    [Fact]
    public async Task GetMoviesAsync_CombinedFilters_ReturnsFilteredIntersection()
    {
        // Search: "one", Genre: "action", Type: "series", Year: 1999
        var parameters = new MovieQueryParameters
        {
            Search = "one",
            GenreId = 1,
            Year = 1999,
            Type = "series",
            SortBy = "rating"
        };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Single(result.Items);
        Assert.Equal("One Piece", result.Items.First().Title);
    }

    // 12. Invalid page
    [Fact]
    public async Task GetMoviesAsync_InvalidPage_ThrowsValidationException()
    {
        var parameters = new MovieQueryParameters { Page = 0 };
        await Assert.ThrowsAsync<ValidationException>(() => _movieService.GetMoviesAsync(parameters));
    }

    // 13. Invalid page size (< 1)
    [Fact]
    public async Task GetMoviesAsync_InvalidPageSize_ThrowsValidationException()
    {
        var parameters = new MovieQueryParameters { PageSize = -5 };
        await Assert.ThrowsAsync<ValidationException>(() => _movieService.GetMoviesAsync(parameters));
    }

    // 14. Maximum page size (> 100)
    [Fact]
    public async Task GetMoviesAsync_PageSizeExceedingMax_ThrowsValidationException()
    {
        var parameters = new MovieQueryParameters { PageSize = 101 };
        await Assert.ThrowsAsync<ValidationException>(() => _movieService.GetMoviesAsync(parameters));
    }

    // 15. Empty search result
    [Fact]
    public async Task GetMoviesAsync_NoMatches_ReturnsEmptyListWithZeroTotal()
    {
        var parameters = new MovieQueryParameters { Search = "nonexistentmoviequery12345" };
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
    }

    // 16. Movie found by ID
    [Fact]
    public async Task GetMovieByIdAsync_ExistingId_ReturnsDetailDtoWithGenres()
    {
        var movie = await _movieService.GetMovieByIdAsync(3);

        Assert.NotNull(movie);
        Assert.Equal("Inception", movie.Title);
        Assert.Equal("inception", movie.Slug);
        Assert.NotEmpty(movie.Genres);
    }

    // 17. Movie not found by ID
    [Fact]
    public async Task GetMovieByIdAsync_NonExistentId_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _movieService.GetMovieByIdAsync(99999));
    }

    // 18. Movie found by slug
    [Fact]
    public async Task GetMovieBySlugAsync_ExistingSlug_ReturnsDetailDto()
    {
        var movie = await _movieService.GetMovieBySlugAsync("interstellar");

        Assert.NotNull(movie);
        Assert.Equal(4, movie.Id);
        Assert.Equal("Interstellar", movie.Title);
    }

    [Fact]
    public async Task GetMovieBySlugAsync_NonExistentSlug_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _movieService.GetMovieBySlugAsync("unknown-movie-slug"));
    }

    // 19. Genre list
    [Fact]
    public async Task GetAllGenresAsync_ReturnsAllSeededGenres()
    {
        var genres = await _genreService.GetAllGenresAsync();

        Assert.NotNull(genres);
        Assert.Equal(4, genres.Count());
        Assert.Contains(genres, g => g.Slug == "action");
        Assert.Contains(genres, g => g.Slug == "drama");
    }

    // 20. Empty database behavior
    [Fact]
    public async Task GetMoviesAsync_EmptyDatabase_ReturnsEmptyCollectionWithZeroTotal()
    {
        // Clear all movies and related records
        _context.EpisodeSources.RemoveRange(_context.EpisodeSources);
        _context.Episodes.RemoveRange(_context.Episodes);
        _context.MovieGenres.RemoveRange(_context.MovieGenres);
        _context.Movies.RemoveRange(_context.Movies);
        await _context.SaveChangesAsync();

        var parameters = new MovieQueryParameters();
        var result = await _movieService.GetMoviesAsync(parameters);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalItems);
        Assert.Equal(0, result.TotalPages);
    }
}
