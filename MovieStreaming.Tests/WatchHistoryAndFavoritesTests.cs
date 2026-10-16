using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieStreaming.Controllers;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Favorites;
using MovieStreaming.DTOs.WatchHistory;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services.Authentication;
using MovieStreaming.Services.Favorites;
using MovieStreaming.Services.WatchHistory;

namespace MovieStreaming.Tests;

public class WatchHistoryAndFavoritesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly WatchHistoryRepository _watchHistoryRepository;
    private readonly FavoriteRepository _favoriteRepository;
    private readonly MovieRepository _movieRepository;
    private readonly EpisodeRepository _episodeRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly WatchHistoryService _watchHistoryService;
    private readonly FavoriteService _favoriteService;
    private readonly WatchHistoryController _watchHistoryController;
    private readonly FavoritesController _favoritesController;

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

    public WatchHistoryAndFavoritesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestApplicationDbContext(options);
        _context.Database.EnsureCreated();

        SeedTestData();

        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockCurrentUserService.Setup(u => u.UserId).Returns(1);
        _mockCurrentUserService.Setup(u => u.IsAuthenticated).Returns(true);

        _watchHistoryRepository = new WatchHistoryRepository(_context);
        _favoriteRepository = new FavoriteRepository(_context);
        _movieRepository = new MovieRepository(_context);
        _episodeRepository = new EpisodeRepository(_context);

        _watchHistoryService = new WatchHistoryService(_watchHistoryRepository, _episodeRepository, _mockCurrentUserService.Object);
        _favoriteService = new FavoriteService(_favoriteRepository, _movieRepository, _mockCurrentUserService.Object);

        _watchHistoryController = new WatchHistoryController(_watchHistoryService);
        _favoritesController = new FavoritesController(_favoriteService);
    }

    private void SeedTestData()
    {
        _context.Favorites.RemoveRange(_context.Favorites);
        _context.WatchHistories.RemoveRange(_context.WatchHistories);
        _context.EpisodeSources.RemoveRange(_context.EpisodeSources);
        _context.Episodes.RemoveRange(_context.Episodes);
        _context.MovieGenres.RemoveRange(_context.MovieGenres);
        _context.Movies.RemoveRange(_context.Movies);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        // 1. Users
        var user1 = new User { Id = 1, Username = "alice", Email = "alice@example.com", PasswordHash = "hash1", RoleId = 1 };
        var user2 = new User { Id = 2, Username = "bob", Email = "bob@example.com", PasswordHash = "hash2", RoleId = 1 };
        _context.Users.AddRange(user1, user2);

        // 2. Movies
        var movie1 = new Movie
        {
            Id = 10,
            Title = "Inception",
            Slug = "inception",
            ReleaseYear = 2010,
            Duration = 148,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        var movie2 = new Movie
        {
            Id = 20,
            Title = "Interstellar",
            Slug = "interstellar",
            ReleaseYear = 2014,
            Duration = 169,
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        _context.Movies.AddRange(movie1, movie2);

        // 3. Episodes
        var ep101 = new Episode
        {
            Id = 101,
            MovieId = 10,
            EpisodeNumber = 1,
            Title = "Episode 1",
            Duration = 60,
            DisplayOrder = 1
        };

        var ep102 = new Episode
        {
            Id = 102,
            MovieId = 10,
            EpisodeNumber = 2,
            Title = "Episode 2",
            Duration = 60,
            DisplayOrder = 2
        };

        var ep201 = new Episode
        {
            Id = 201,
            MovieId = 20,
            EpisodeNumber = 1,
            Title = "Episode 1",
            Duration = 50,
            DisplayOrder = 1
        };

        _context.Episodes.AddRange(ep101, ep102, ep201);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // 1. Authenticated user can create watch history
    [Fact]
    public async Task SaveWatchProgress_NewRecord_CreatesWatchHistorySuccessfully()
    {
        var request = new SaveWatchHistoryRequest
        {
            EpisodeId = 101,
            ProgressSeconds = 300,
            DurationSeconds = 3600
        };

        var result = await _watchHistoryService.SaveWatchProgressAsync(request);

        Assert.NotNull(result);
        Assert.Equal(101, result.EpisodeId);
        Assert.Equal(10, result.MovieId);
        Assert.Equal(300, result.ProgressSeconds);
        Assert.Equal(3600, result.DurationSeconds);
        Assert.False(result.Completed);

        var inDb = await _context.WatchHistories.FirstOrDefaultAsync(wh => wh.UserId == 1 && wh.EpisodeId == 101);
        Assert.NotNull(inDb);
        Assert.Equal(300, inDb.ProgressSeconds);
    }

    // 2. Authenticated user can update existing watch history
    [Fact]
    public async Task SaveWatchProgress_ExistingRecord_UpdatesProgressInsteadOfDuplicating()
    {
        var request1 = new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = 300, DurationSeconds = 3600 };
        await _watchHistoryService.SaveWatchProgressAsync(request1);

        var request2 = new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = 1200, DurationSeconds = 3600 };
        var updated = await _watchHistoryService.SaveWatchProgressAsync(request2);

        Assert.Equal(1200, updated.ProgressSeconds);

        var records = await _context.WatchHistories.Where(wh => wh.UserId == 1 && wh.EpisodeId == 101).ToListAsync();
        Assert.Single(records);
        Assert.Equal(1200, records[0].ProgressSeconds);
    }

    // 3. Duplicate user + episode does not create another record
    [Fact]
    public async Task SaveWatchProgress_RepeatedCalls_MaintainsSingleRow()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest
            {
                EpisodeId = 101,
                ProgressSeconds = i * 100,
                DurationSeconds = 3600
            });
        }

        var count = await _context.WatchHistories.CountAsync(wh => wh.UserId == 1 && wh.EpisodeId == 101);
        Assert.Equal(1, count);
    }

    // 4. User can retrieve own history
    [Fact]
    public async Task GetWatchHistory_ReturnsUsersOwnHistory()
    {
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = 500, DurationSeconds = 3600 });
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 102, ProgressSeconds = 200, DurationSeconds = 3600 });

        var history = await _watchHistoryService.GetWatchHistoryAsync(1, 20);

        Assert.NotNull(history);
        Assert.Equal(2, history.TotalItems);
        Assert.Equal(2, history.Items.Count);
        Assert.Contains(history.Items, h => h.EpisodeId == 101);
        Assert.Contains(history.Items, h => h.EpisodeId == 102);
    }

    // 5. History is sorted by LastWatchedAt descending
    [Fact]
    public async Task GetWatchHistory_IsSortedByLastWatchedAtDescending()
    {
        var wh1 = new WatchHistory
        {
            UserId = 1,
            MovieId = 10,
            EpisodeId = 101,
            ProgressSeconds = 100,
            DurationSeconds = 1000,
            LastWatchedAt = DateTime.UtcNow.AddHours(-2)
        };
        var wh2 = new WatchHistory
        {
            UserId = 1,
            MovieId = 10,
            EpisodeId = 102,
            ProgressSeconds = 200,
            DurationSeconds = 1000,
            LastWatchedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        _context.WatchHistories.AddRange(wh1, wh2);
        await _context.SaveChangesAsync();

        var history = await _watchHistoryService.GetWatchHistoryAsync(1, 20);

        Assert.Equal(102, history.Items[0].EpisodeId);
        Assert.Equal(101, history.Items[1].EpisodeId);
    }

    // 6. Pagination works
    [Fact]
    public async Task GetWatchHistory_Pagination_ReturnsCorrectPageAndPageSize()
    {
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = 100 });
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 102, ProgressSeconds = 200 });
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 201, ProgressSeconds = 300 });

        var page1 = await _watchHistoryService.GetWatchHistoryAsync(page: 1, pageSize: 2);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(3, page1.TotalItems);
        Assert.Equal(2, page1.TotalPages);

        var page2 = await _watchHistoryService.GetWatchHistoryAsync(page: 2, pageSize: 2);
        Assert.Single(page2.Items);
    }

    // 7. Continue watching excludes completed episodes (>= 90%)
    [Fact]
    public async Task GetContinueWatching_ExcludesCompletedEpisodes()
    {
        // Ep 101: 900s of 1000s = 90% (Completed)
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = 900, DurationSeconds = 1000 });
        // Ep 102: 400s of 1000s = 40% (Incomplete)
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 102, ProgressSeconds = 400, DurationSeconds = 1000 });

        var continueList = await _watchHistoryService.GetContinueWatchingAsync();

        Assert.Single(continueList);
        Assert.Equal(102, continueList[0].EpisodeId);
        Assert.False(continueList[0].Completed);
    }

    // 8. User can delete own history
    [Fact]
    public async Task DeleteWatchHistoryItem_RemovesRecordSuccessfully()
    {
        await _watchHistoryService.SaveWatchProgressAsync(new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = 500 });

        await _watchHistoryService.DeleteWatchHistoryItemAsync(101);

        var exists = await _context.WatchHistories.AnyAsync(wh => wh.UserId == 1 && wh.EpisodeId == 101);
        Assert.False(exists);
    }

    // 9. User cannot access or delete another user's history
    [Fact]
    public async Task UserIsolation_User1CannotSeeOrDeleteUser2History()
    {
        // User 2 saves history
        var user2Record = new WatchHistory
        {
            UserId = 2,
            MovieId = 10,
            EpisodeId = 101,
            ProgressSeconds = 777,
            DurationSeconds = 1000,
            LastWatchedAt = DateTime.UtcNow
        };
        _context.WatchHistories.Add(user2Record);
        await _context.SaveChangesAsync();

        // Current user is User 1
        var user1History = await _watchHistoryService.GetWatchHistoryAsync();
        Assert.Empty(user1History.Items);

        // User 1 attempts to delete User 2's episode record -> 404 Not Found for User 1
        await Assert.ThrowsAsync<NotFoundException>(() => _watchHistoryService.DeleteWatchHistoryItemAsync(101));

        // User 2's record remains untouched
        var user2Exists = await _context.WatchHistories.AnyAsync(wh => wh.UserId == 2 && wh.EpisodeId == 101);
        Assert.True(user2Exists);
    }

    // 10. Invalid episode is rejected
    [Fact]
    public async Task SaveWatchProgress_NonExistentEpisode_ThrowsNotFoundException()
    {
        var request = new SaveWatchHistoryRequest { EpisodeId = 99999, ProgressSeconds = 100 };
        await Assert.ThrowsAsync<NotFoundException>(() => _watchHistoryService.SaveWatchProgressAsync(request));
    }

    [Fact]
    public async Task SaveWatchProgress_InvalidEpisodeId_ThrowsValidationException()
    {
        var request = new SaveWatchHistoryRequest { EpisodeId = 0, ProgressSeconds = 100 };
        await Assert.ThrowsAsync<ValidationException>(() => _watchHistoryService.SaveWatchProgressAsync(request));
    }

    // 11. Negative progress is rejected
    [Fact]
    public async Task SaveWatchProgress_NegativeProgress_ThrowsValidationException()
    {
        var request = new SaveWatchHistoryRequest { EpisodeId = 101, ProgressSeconds = -10 };
        await Assert.ThrowsAsync<ValidationException>(() => _watchHistoryService.SaveWatchProgressAsync(request));
    }

    // 12. Progress calculation works
    [Fact]
    public async Task WatchProgressRules_CalculatePercentage_CalculatesCorrectValue()
    {
        var pct = WatchProgressRules.CalculateProgressPercentage(450, 1000);
        Assert.Equal(45.0, pct);

        var pct2 = WatchProgressRules.CalculateProgressPercentage(1000, 1000);
        Assert.Equal(100.0, pct2);

        var pctNull = WatchProgressRules.CalculateProgressPercentage(500, null);
        Assert.Equal(0.0, pctNull);
    }

    // 13. Completion calculation works (90% rule)
    [Fact]
    public async Task WatchProgressRules_IsCompleted_CalculatesAccurately()
    {
        Assert.False(WatchProgressRules.IsCompleted(899, 1000));
        Assert.True(WatchProgressRules.IsCompleted(900, 1000));
        Assert.True(WatchProgressRules.IsCompleted(950, 1000));
        Assert.False(WatchProgressRules.IsCompleted(500, null));
        Assert.False(WatchProgressRules.IsCompleted(500, 0));
    }

    // 14. User can add a favorite
    [Fact]
    public async Task AddFavorite_NewMovie_AddsFavoriteSuccessfully()
    {
        var result = await _favoriteService.AddFavoriteAsync(10);

        Assert.NotNull(result);
        Assert.Equal(10, result.MovieId);
        Assert.Equal("Inception", result.Title);

        var inDb = await _context.Favorites.AnyAsync(f => f.UserId == 1 && f.MovieId == 10);
        Assert.True(inDb);
    }

    // 15. Duplicate favorite does not create another row
    [Fact]
    public async Task AddFavorite_DuplicateCall_IsIdempotentAndCreatesSingleRow()
    {
        await _favoriteService.AddFavoriteAsync(10);
        await _favoriteService.AddFavoriteAsync(10);

        var count = await _context.Favorites.CountAsync(f => f.UserId == 1 && f.MovieId == 10);
        Assert.Equal(1, count);
    }

    // 16. User can remove a favorite
    [Fact]
    public async Task RemoveFavorite_ExistingFavorite_RemovesSuccessfully()
    {
        await _favoriteService.AddFavoriteAsync(10);

        var removed = await _favoriteService.RemoveFavoriteAsync(10);
        Assert.True(removed);

        var exists = await _context.Favorites.AnyAsync(f => f.UserId == 1 && f.MovieId == 10);
        Assert.False(exists);
    }

    // 17. User can retrieve own favorites
    [Fact]
    public async Task GetFavorites_ReturnsUsersOwnFavorites()
    {
        await _favoriteService.AddFavoriteAsync(10);
        await _favoriteService.AddFavoriteAsync(20);

        var favorites = await _favoriteService.GetFavoritesAsync();

        Assert.Equal(2, favorites.TotalItems);
        Assert.Contains(favorites.Items, f => f.MovieId == 10);
        Assert.Contains(favorites.Items, f => f.MovieId == 20);
    }

    // 18. Favorites are sorted correctly (CreatedAt DESC)
    [Fact]
    public async Task GetFavorites_IsSortedByCreatedAtDescending()
    {
        var fav1 = new Favorite { UserId = 1, MovieId = 10, CreatedAt = DateTime.UtcNow.AddDays(-2) };
        var fav2 = new Favorite { UserId = 1, MovieId = 20, CreatedAt = DateTime.UtcNow.AddMinutes(-5) };

        _context.Favorites.AddRange(fav1, fav2);
        await _context.SaveChangesAsync();

        var favorites = await _favoriteService.GetFavoritesAsync();

        Assert.Equal(20, favorites.Items[0].MovieId);
        Assert.Equal(10, favorites.Items[1].MovieId);
    }

    // 19. Favorite status works
    [Fact]
    public async Task GetFavoriteStatus_ReturnsCorrectStatus()
    {
        var statusBefore = await _favoriteService.GetFavoriteStatusAsync(10);
        Assert.False(statusBefore.IsFavorite);

        await _favoriteService.AddFavoriteAsync(10);

        var statusAfter = await _favoriteService.GetFavoriteStatusAsync(10);
        Assert.True(statusAfter.IsFavorite);
    }

    // 20. User cannot manipulate another user's favorites
    [Fact]
    public async Task UserIsolation_User1CannotRemoveOrSeeUser2Favorites()
    {
        // User 2 adds favorite
        var favUser2 = new Favorite { UserId = 2, MovieId = 10, CreatedAt = DateTime.UtcNow };
        _context.Favorites.Add(favUser2);
        await _context.SaveChangesAsync();

        // User 1 sees no favorites
        var user1Favorites = await _favoriteService.GetFavoritesAsync();
        Assert.Empty(user1Favorites.Items);

        // User 1 checks status: Movie 10 is NOT favorited by User 1
        var status = await _favoriteService.GetFavoriteStatusAsync(10);
        Assert.False(status.IsFavorite);

        // User 1 attempts to remove -> throws 404 NotFound for User 1
        await Assert.ThrowsAsync<NotFoundException>(() => _favoriteService.RemoveFavoriteAsync(10));

        // User 2's favorite remains in DB
        var user2StillHasFav = await _context.Favorites.AnyAsync(f => f.UserId == 2 && f.MovieId == 10);
        Assert.True(user2StillHasFav);
    }

    // 21. Invalid movie ID is handled correctly
    [Fact]
    public async Task AddFavorite_NonExistentMovie_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _favoriteService.AddFavoriteAsync(99999));
    }

    [Fact]
    public async Task AddFavorite_InvalidMovieId_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() => _favoriteService.AddFavoriteAsync(0));
    }

    // Controller End-to-End Tests
    [Fact]
    public async Task WatchHistoryController_SaveAndGet_ReturnsOk()
    {
        var saveResult = await _watchHistoryController.SaveProgress(new SaveWatchHistoryRequest
        {
            EpisodeId = 101,
            ProgressSeconds = 250,
            DurationSeconds = 1000
        }, CancellationToken.None);

        var okSave = Assert.IsType<OkObjectResult>(saveResult.Result);
        var item = Assert.IsType<WatchHistoryItemDto>(okSave.Value);
        Assert.Equal(101, item.EpisodeId);

        var getResult = await _watchHistoryController.GetHistory(1, 20, CancellationToken.None);
        var okGet = Assert.IsType<OkObjectResult>(getResult.Result);
        var response = Assert.IsType<WatchHistoryResponseDto>(okGet.Value);
        Assert.Single(response.Items);

        var deleteResult = await _watchHistoryController.DeleteHistoryItem(101, CancellationToken.None);
        Assert.IsType<OkObjectResult>(deleteResult);
    }

    [Fact]
    public async Task FavoritesController_AddAndStatus_ReturnsOk()
    {
        var addResult = await _favoritesController.AddFavorite(10, CancellationToken.None);
        var okAdd = Assert.IsType<OkObjectResult>(addResult.Result);
        var movie = Assert.IsType<FavoriteMovieDto>(okAdd.Value);
        Assert.Equal(10, movie.MovieId);

        var statusResult = await _favoritesController.GetFavoriteStatus(10, CancellationToken.None);
        var okStatus = Assert.IsType<OkObjectResult>(statusResult.Result);
        var status = Assert.IsType<FavoriteStatusDto>(okStatus.Value);
        Assert.True(status.IsFavorite);

        var removeResult = await _favoritesController.RemoveFavorite(10, CancellationToken.None);
        Assert.IsType<OkObjectResult>(removeResult);
    }
}
