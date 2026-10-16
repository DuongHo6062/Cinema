using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MovieStreaming.Data;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.KKPhim;
using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Tests;

public class KKPhimImportServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly Mock<IKKPhimClient> _mockClient;
    private readonly KKPhimImportService _importService;

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

    public KKPhimImportServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _mockClient = new Mock<IKKPhimClient>();
        _importService = new KKPhimImportService(
            _context,
            _mockClient.Object,
            NullLogger<KKPhimImportService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_NewMovie_InsertsMovieWithGenresEpisodesAndSources()
    {
        // Arrange
        var slug = "test-movie";
        var detailResponse = new KKPhimMovieDetailResponse
        {
            Status = true,
            Movie = new KKPhimMovieDetailItem
            {
                Id = "ext-100",
                Name = "Test Movie Title",
                Slug = slug,
                OriginName = "Original Test Title",
                Content = "<p>Test description content</p>",
                Type = "series",
                Status = "ongoing",
                Quality = "FHD",
                Lang = "Vietsub",
                Year = 2026,
                Time = "45 phút/tập",
                Category = new List<KKPhimCategoryItem>
                {
                    new() { Name = "Hành Động", Slug = "hanh-dong" },
                    new() { Name = "Phiêu Lưu", Slug = "phieu-luu" }
                }
            },
            Episodes = new List<KKPhimServerItem>
            {
                new()
                {
                    ServerName = "Vietsub",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new()
                        {
                            Name = "Tập 01",
                            Slug = "tap-01",
                            Filename = "Episode 01 filename",
                            LinkM3u8 = "https://example.com/ep01.m3u8",
                            LinkEmbed = "https://example.com/embed/ep01"
                        }
                    }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetMovieDetailAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act
        var result = await _importService.ImportMovieBySlugAsync(slug);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsNew);
        Assert.Equal("Test Movie Title", result.Title);
        Assert.Equal(1, result.EpisodesImported);
        Assert.Equal(1, result.SourcesImported);

        // Verify in database
        var movieInDb = await _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(m => m.Episodes).ThenInclude(e => e.Sources)
            .FirstOrDefaultAsync(m => m.Slug == slug);

        Assert.NotNull(movieInDb);
        Assert.Equal("ext-100", movieInDb.ExternalId);
        Assert.Equal("KKPHIM", movieInDb.ExternalSource);
        Assert.Equal(2, movieInDb.MovieGenres.Count);
        Assert.Single(movieInDb.Episodes);
        Assert.Single(movieInDb.Episodes.First().Sources);
        Assert.Equal("https://example.com/ep01.m3u8", movieInDb.Episodes.First().Sources.First().VideoUrl);
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_ExistingMovie_UpdatesMovieWithoutDuplicates()
    {
        // Arrange: Pre-populate existing movie
        var existingMovie = new Movie
        {
            Title = "Old Title",
            Slug = "existing-movie",
            ExternalId = "ext-200",
            ExternalSource = "KKPHIM",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Movies.Add(existingMovie);
        await _context.SaveChangesAsync();

        var detailResponse = new KKPhimMovieDetailResponse
        {
            Status = true,
            Movie = new KKPhimMovieDetailItem
            {
                Id = "ext-200",
                Name = "Updated Movie Title",
                Slug = "existing-movie",
                OriginName = "Updated Origin",
                Type = "single",
                Year = 2025
            },
            Episodes = new List<KKPhimServerItem>
            {
                new()
                {
                    ServerName = "Vietsub",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new()
                        {
                            Name = "Full",
                            Slug = "full",
                            LinkM3u8 = "https://example.com/full.m3u8"
                        }
                    }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetMovieDetailAsync("existing-movie", It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act: Import first time (update)
        var result1 = await _importService.ImportMovieBySlugAsync("existing-movie");

        // Act: Import second time (re-import verification)
        var result2 = await _importService.ImportMovieBySlugAsync("existing-movie");

        // Assert
        Assert.False(result1.IsNew);
        Assert.False(result2.IsNew);

        var moviesCount = await _context.Movies.CountAsync(m => m.ExternalId == "ext-200");
        Assert.Equal(1, moviesCount);

        var updatedMovie = await _context.Movies
            .Include(m => m.Episodes).ThenInclude(e => e.Sources)
            .FirstAsync(m => m.ExternalId == "ext-200");

        Assert.Equal("Updated Movie Title", updatedMovie.Title);
        Assert.Single(updatedMovie.Episodes);
        Assert.Single(updatedMovie.Episodes.First().Sources);
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_MultipleServers_PreservesAllSources()
    {
        // Arrange
        var slug = "multi-server-movie";
        var detailResponse = new KKPhimMovieDetailResponse
        {
            Status = true,
            Movie = new KKPhimMovieDetailItem
            {
                Id = "ext-300",
                Name = "Multi Server Movie",
                Slug = slug
            },
            Episodes = new List<KKPhimServerItem>
            {
                new()
                {
                    ServerName = "Vietsub",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new()
                        {
                            Name = "Tập 01",
                            Slug = "tap-01",
                            LinkM3u8 = "https://example.com/vietsub_ep01.m3u8"
                        }
                    }
                },
                new()
                {
                    ServerName = "Thuyết Minh",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new()
                        {
                            Name = "Tập 01",
                            Slug = "tap-01",
                            LinkM3u8 = "https://example.com/thuyetminh_ep01.m3u8"
                        }
                    }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetMovieDetailAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act - with vietsubOnly = false, preserves all sources
        var result = await _importService.ImportMovieBySlugAsync(slug, vietsubOnly: false, englishOnly: false);

        // Assert
        Assert.True(result.Success);
        var movie = await _context.Movies
            .Include(m => m.Episodes).ThenInclude(e => e.Sources)
            .FirstAsync(m => m.Slug == slug);

        // Only 1 episode entity for Episode 1
        Assert.Single(movie.Episodes);

        var episode = movie.Episodes.First();
        // 2 separate sources: Vietsub & Thuyết Minh preserved
        Assert.Equal(2, episode.Sources.Count);
        Assert.Contains(episode.Sources, s => s.ServerName == "Vietsub" && s.VideoUrl.Contains("vietsub"));
        Assert.Contains(episode.Sources, s => s.ServerName == "Thuyết Minh" && s.VideoUrl.Contains("thuyetminh"));
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_BothVersions_ImportsVietsubOnly()
    {
        // Arrange
        var slug = "oppenheimer-both-versions";
        var detailResponse = new KKPhimMovieDetailResponse
        {
            Status = true,
            Movie = new KKPhimMovieDetailItem
            {
                Id = "kk-oppenheimer-1",
                Name = "Oppenheimer",
                OriginName = "Oppenheimer",
                Slug = slug,
                Type = "single",
                Status = "completed",
                Year = 2023,
                Country = new List<KKPhimCountryItem> { new() { Id = "us", Name = "Âu Mỹ", Slug = "au-my" } }
            },
            Episodes = new List<KKPhimServerItem>
            {
                new()
                {
                    ServerName = "Vietsub",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new() { Name = "Full", Slug = "full", LinkM3u8 = "https://example.com/vietsub.m3u8" }
                    }
                },
                new()
                {
                    ServerName = "Thuyết Minh",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new() { Name = "Full", Slug = "full", LinkM3u8 = "https://example.com/thuyetminh.m3u8" }
                    }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetMovieDetailAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act - default vietsubOnly is true
        var result = await _importService.ImportMovieBySlugAsync(slug, vietsubOnly: true, englishOnly: true);

        // Assert
        Assert.True(result.Success);
        var movie = await _context.Movies
            .Include(m => m.Episodes).ThenInclude(e => e.Sources)
            .FirstAsync(m => m.Slug == slug);

        Assert.True(movie.IsVietsub);
        var episode = movie.Episodes.First();
        Assert.Single(episode.Sources);
        Assert.Equal("Vietsub", episode.Sources.First().ServerName);
        Assert.DoesNotContain(episode.Sources, s => s.ServerName.Contains("Thuyết Minh"));
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_NonEnglishMovie_RejectsWhenEnglishOnly()
    {
        // Arrange
        var slug = "korean-movie";
        var detailResponse = new KKPhimMovieDetailResponse
        {
            Status = true,
            Movie = new KKPhimMovieDetailItem
            {
                Id = "kk-korean-1",
                Name = "Phim Hàn Quốc",
                OriginName = "기생충",
                Slug = slug,
                Type = "single",
                Status = "completed",
                Year = 2022,
                Country = new List<KKPhimCountryItem> { new() { Id = "kr", Name = "Hàn Quốc", Slug = "han-quoc" } }
            },
            Episodes = new List<KKPhimServerItem>
            {
                new()
                {
                    ServerName = "Vietsub",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new() { Name = "Full", Slug = "full", LinkM3u8 = "https://example.com/korean_vietsub.m3u8" }
                    }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetMovieDetailAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _importService.ImportMovieBySlugAsync(slug, vietsubOnly: true, englishOnly: true));
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_DubbedOnlyMovie_RejectsWhenVietsubOnly()
    {
        // Arrange
        var slug = "dubbed-only-movie";
        var detailResponse = new KKPhimMovieDetailResponse
        {
            Status = true,
            Movie = new KKPhimMovieDetailItem
            {
                Id = "kk-dubbed-1",
                Name = "Phim Thuyết Minh",
                OriginName = "Dubbed Movie",
                Slug = slug,
                Type = "single",
                Status = "completed",
                Year = 2023,
                Country = new List<KKPhimCountryItem> { new() { Id = "us", Name = "Âu Mỹ", Slug = "au-my" } }
            },
            Episodes = new List<KKPhimServerItem>
            {
                new()
                {
                    ServerName = "Thuyết Minh #1",
                    ServerData = new List<KKPhimServerDataItem>
                    {
                        new() { Name = "Full", Slug = "full", LinkM3u8 = "https://example.com/thuyetminh.m3u8" }
                    }
                }
            }
        };

        _mockClient
            .Setup(c => c.GetMovieDetailAsync(slug, It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _importService.ImportMovieBySlugAsync(slug, vietsubOnly: true, englishOnly: true));
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_NotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mockClient
            .Setup(c => c.GetMovieDetailAsync("non-existent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((KKPhimMovieDetailResponse?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _importService.ImportMovieBySlugAsync("non-existent"));
    }

    [Fact]
    public async Task ImportMovieBySlugAsync_EmptySlug_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _importService.ImportMovieBySlugAsync("   "));
    }
}
