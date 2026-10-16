using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MovieStreaming.Controllers;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Episodes;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services;

namespace MovieStreaming.Tests;

public class MovieDetailAndVideoPlayerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly MovieRepository _movieRepository;
    private readonly MovieService _movieService;
    private readonly EpisodeRepository _episodeRepository;
    private readonly EpisodeService _episodeService;
    private readonly MoviesController _moviesController;
    private readonly EpisodesController _episodesController;

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

    public MovieDetailAndVideoPlayerTests()
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
        _episodeRepository = new EpisodeRepository(_context);
        _episodeService = new EpisodeService(_episodeRepository);
        _moviesController = new MoviesController(_movieService, _episodeService);
        _episodesController = new EpisodesController(_episodeService);
    }

    private void SeedTestData()
    {
        // Clear any seed data
        _context.EpisodeSources.RemoveRange(_context.EpisodeSources);
        _context.Episodes.RemoveRange(_context.Episodes);
        _context.MovieGenres.RemoveRange(_context.MovieGenres);
        _context.Movies.RemoveRange(_context.Movies);
        _context.Genres.RemoveRange(_context.Genres);
        _context.SaveChanges();

        // 1. Movie with multiple episodes and multiple sources (including HLS, MP4, and invalid empty source)
        var movie1 = new Movie
        {
            Id = 10,
            Title = "Inception",
            Slug = "inception",
            OriginalTitle = "Inception",
            Description = "A thief who steals corporate secrets...",
            PosterUrl = "https://example.com/inception-poster.jpg",
            ThumbnailUrl = "https://example.com/inception-thumb.jpg",
            ReleaseYear = 2010,
            Duration = 148,
            Country = "USA",
            Language = "English",
            MovieType = "single",
            Status = "completed",
            Quality = "HD",
            Rating = 8.8m,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var ep101 = new Episode
        {
            Id = 101,
            MovieId = 10,
            EpisodeNumber = 1,
            SeasonNumber = 1,
            DisplayOrder = 1,
            Title = "Part 1 - Dream Entering",
            Slug = "part-1",
            Duration = 70,
            Description = "The introduction to the dream invasion team.",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var src1 = new EpisodeSource
        {
            Id = 1001,
            EpisodeId = 101,
            ServerName = "Server VIP",
            Language = "Vietsub",
            SourceType = "m3u8",
            VideoUrl = "https://stream.example.com/hls/ep101.m3u8",
            EmbedUrl = "https://player.example.com/?url=ep101",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var src2 = new EpisodeSource
        {
            Id = 1002,
            EpisodeId = 101,
            ServerName = "Server Backup",
            Language = "Thuyết Minh",
            SourceType = "mp4",
            VideoUrl = "https://stream.example.com/mp4/ep101.mp4",
            EmbedUrl = null,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var srcEmpty = new EpisodeSource
        {
            Id = 1003,
            EpisodeId = 101,
            ServerName = "Server Broken",
            Language = "English",
            SourceType = "HLS",
            VideoUrl = "   ", // whitespace URL that should be filtered out
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var ep102 = new Episode
        {
            Id = 102,
            MovieId = 10,
            EpisodeNumber = 2,
            SeasonNumber = 1,
            DisplayOrder = 2,
            Title = "Part 2 - Limbo",
            Slug = "part-2",
            Duration = 78,
            Description = "The final confrontation inside limbo.",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var src3 = new EpisodeSource
        {
            Id = 1004,
            EpisodeId = 102,
            ServerName = "Server VIP",
            Language = "Vietsub",
            SourceType = "m3u8",
            VideoUrl = "https://stream.example.com/hls/ep102.m3u8",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        // 2. Movie with out-of-order episodes to test deterministic sorting
        var movie2 = new Movie
        {
            Id = 20,
            Title = "Breaking Bad",
            Slug = "breaking-bad",
            MovieType = "series",
            ReleaseYear = 2008,
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var ep203 = new Episode
        {
            Id = 203,
            MovieId = 20,
            EpisodeNumber = 3,
            DisplayOrder = 3,
            Title = "Episode 3",
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var ep201 = new Episode
        {
            Id = 201,
            MovieId = 20,
            EpisodeNumber = 1,
            DisplayOrder = 1,
            Title = "Episode 1",
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var ep202 = new Episode
        {
            Id = 202,
            MovieId = 20,
            EpisodeNumber = 2,
            DisplayOrder = 2,
            Title = "Episode 2",
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        // 3. Movie with no episodes
        var movie3 = new Movie
        {
            Id = 30,
            Title = "Upcoming Film",
            Slug = "upcoming-film",
            MovieType = "single",
            ReleaseYear = 2026,
            CreatedAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc)
        };

        _context.Movies.AddRange(movie1, movie2, movie3);
        _context.Episodes.AddRange(ep101, ep102, ep203, ep201, ep202);
        _context.EpisodeSources.AddRange(src1, src2, srcEmpty, src3);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // 1. Movie detail found
    [Fact]
    public async Task GetMovieById_Found_ReturnsMovieDetailWithEpisodes()
    {
        var result = await _movieService.GetMovieByIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("Inception", result.Title);
        Assert.Equal(2, result.Episodes.Count);
        Assert.Equal("Part 1 - Dream Entering", result.Episodes[0].EpisodeName);
        Assert.Equal(1, result.Episodes[0].DisplayOrder);
    }

    // 2. Movie detail not found
    [Fact]
    public async Task GetMovieById_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _movieService.GetMovieByIdAsync(999));
    }

    // 3. Movie found by slug
    [Fact]
    public async Task GetMovieBySlug_Found_ReturnsMovieDetail()
    {
        var result = await _movieService.GetMovieBySlugAsync("inception");

        Assert.NotNull(result);
        Assert.Equal(10, result.Id);
        Assert.Equal("inception", result.Slug);
        Assert.Equal(2, result.Episodes.Count);
    }

    // 4. Movie episodes returned
    [Fact]
    public async Task GetEpisodesByMovieId_ExistingMovie_ReturnsEpisodeList()
    {
        var episodes = await _episodeService.GetEpisodesByMovieIdAsync(10);

        Assert.NotNull(episodes);
        Assert.Equal(2, episodes.Count);
        Assert.Equal(101, episodes[0].Id);
        Assert.Equal(102, episodes[1].Id);
        Assert.Equal("Part 1 - Dream Entering", episodes[0].EpisodeName);
        Assert.Equal("Part 1 - Dream Entering", episodes[0].Title);
        Assert.Equal(1, episodes[0].DisplayOrder);
    }

    // 5. Episodes are correctly ordered
    [Fact]
    public async Task GetEpisodesByMovieId_OutOfOrderDatabaseRecords_ReturnsCorrectlyOrderedList()
    {
        var episodes = await _episodeService.GetEpisodesByMovieIdAsync(20);

        Assert.NotNull(episodes);
        Assert.Equal(3, episodes.Count);
        // Seeded 203, 201, 202 - must return 201 (ep 1), 202 (ep 2), 203 (ep 3)
        Assert.Equal(1, episodes[0].EpisodeNumber);
        Assert.Equal(2, episodes[1].EpisodeNumber);
        Assert.Equal(3, episodes[2].EpisodeNumber);
        Assert.Equal(1, episodes[0].DisplayOrder);
        Assert.Equal(2, episodes[1].DisplayOrder);
        Assert.Equal(3, episodes[2].DisplayOrder);
    }

    // 6. Movie with no episodes returns empty list
    [Fact]
    public async Task GetEpisodesByMovieId_MovieWithNoEpisodes_ReturnsEmptyList()
    {
        var episodes = await _episodeService.GetEpisodesByMovieIdAsync(30);

        Assert.NotNull(episodes);
        Assert.Empty(episodes);
    }

    [Fact]
    public async Task GetEpisodesByMovieId_NonExistentMovie_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _episodeService.GetEpisodesByMovieIdAsync(9999));
    }

    [Fact]
    public async Task GetEpisodesByMovieId_InvalidId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _episodeService.GetEpisodesByMovieIdAsync(0));
        await Assert.ThrowsAsync<ValidationException>(() => _episodeService.GetEpisodesByMovieIdAsync(-5));
    }

    // 7. Episode detail found
    [Fact]
    public async Task GetEpisodeById_Found_ReturnsEpisodeDetailWithMovieAndSources()
    {
        var episode = await _episodeService.GetEpisodeByIdAsync(101);

        Assert.NotNull(episode);
        Assert.Equal(101, episode.Id);
        Assert.Equal(10, episode.MovieId);
        Assert.Equal("Inception", episode.MovieTitle);
        Assert.Equal(1, episode.EpisodeNumber);
        Assert.Equal("Part 1 - Dream Entering", episode.EpisodeName);
        Assert.NotEmpty(episode.Sources);
    }

    // 8. Episode not found
    [Fact]
    public async Task GetEpisodeById_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _episodeService.GetEpisodeByIdAsync(99999));
    }

    [Fact]
    public async Task GetEpisodeById_InvalidId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _episodeService.GetEpisodeByIdAsync(0));
    }

    // 9. Episode sources returned
    [Fact]
    public async Task GetEpisodeSources_Found_ReturnsSourcesList()
    {
        var result = await _episodeService.GetEpisodeSourcesAsync(101);

        Assert.NotNull(result);
        Assert.Equal(101, result.EpisodeId);
        Assert.NotEmpty(result.Sources);
    }

    // 10. Multiple video sources returned & ordered deterministically
    [Fact]
    public async Task GetEpisodeSources_MultipleSources_ReturnedDeterministicallyOrdered()
    {
        var result = await _episodeService.GetEpisodeSourcesAsync(101);

        Assert.NotNull(result);
        Assert.Equal(2, result.Sources.Count);
        // Server Backup comes alphabetically before Server VIP
        Assert.Equal("Server Backup", result.Sources[0].ServerName);
        Assert.Equal("Server VIP", result.Sources[1].ServerName);
    }

    // 11. Empty source URL handling
    [Fact]
    public async Task GetEpisodeSources_EmptyOrWhitespaceVideoUrl_IsFilteredOut()
    {
        var result = await _episodeService.GetEpisodeSourcesAsync(101);

        // Database has 3 sources: Server VIP, Server Backup, and Server Broken (whitespace url)
        // Server Broken must be filtered out
        Assert.DoesNotContain(result.Sources, s => s.ServerName == "Server Broken");
        Assert.All(result.Sources, s => Assert.False(string.IsNullOrWhiteSpace(s.VideoUrl)));
    }

    // 12. HLS source returned correctly
    [Fact]
    public async Task GetEpisodeSources_M3u8Url_IsIdentifiedAsHls()
    {
        var result = await _episodeService.GetEpisodeSourcesAsync(101);

        var vipSource = result.Sources.FirstOrDefault(s => s.ServerName == "Server VIP");
        Assert.NotNull(vipSource);
        Assert.Equal("HLS", vipSource.SourceType);
        Assert.Contains(".m3u8", vipSource.VideoUrl);
    }

    // 13. Episode belonging to another movie cannot be incorrectly exposed
    [Fact]
    public async Task GetEpisodeByMovieAndId_BelongingToDifferentMovie_ThrowsNotFoundException()
    {
        // Episode 101 belongs to Movie 10, not Movie 20
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _episodeService.GetEpisodeByMovieAndIdAsync(movieId: 20, episodeId: 101));

        Assert.Contains("does not belong to movie", ex.Message);
    }

    [Fact]
    public async Task GetEpisodeByMovieAndId_BelongingToCorrectMovie_ReturnsDetail()
    {
        var episode = await _episodeService.GetEpisodeByMovieAndIdAsync(movieId: 10, episodeId: 101);

        Assert.NotNull(episode);
        Assert.Equal(101, episode.Id);
        Assert.Equal(10, episode.MovieId);
    }

    // Controller End-to-End Integration
    [Fact]
    public async Task MoviesController_GetMovieEpisodes_ReturnsOk()
    {
        var actionResult = await _moviesController.GetMovieEpisodes(10, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var items = Assert.IsAssignableFrom<IReadOnlyList<EpisodeListItemDto>>(okResult.Value);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task MoviesController_GetMovieEpisodeById_ReturnsOk()
    {
        var actionResult = await _moviesController.GetMovieEpisodeById(10, 101, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var item = Assert.IsType<EpisodeDetailDto>(okResult.Value);

        Assert.Equal(101, item.Id);
        Assert.Equal(10, item.MovieId);
    }

    [Fact]
    public async Task EpisodesController_GetEpisodeById_ReturnsOk()
    {
        var actionResult = await _episodesController.GetEpisodeById(101, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var item = Assert.IsType<EpisodeDetailDto>(okResult.Value);

        Assert.Equal(101, item.Id);
    }

    [Fact]
    public async Task EpisodesController_GetEpisodeSources_ReturnsOk()
    {
        var actionResult = await _episodesController.GetEpisodeSources(101, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var item = Assert.IsType<EpisodeSourcesResponseDto>(okResult.Value);

        Assert.Equal(101, item.EpisodeId);
        Assert.Equal(2, item.Sources.Count);
    }
}
