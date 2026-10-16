using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MovieStreaming.Configurations;
using MovieStreaming.Controllers;
using MovieStreaming.Data;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Recommendations;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Authentication;
using MovieStreaming.Services.Recommendations;
using Xunit;

namespace MovieStreaming.Tests;

public class RecommendationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly RecommendationScorer _scorer;
    private readonly RecommendationService _recommendationService;
    private readonly RecommendationOptions _options;

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

    public RecommendationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestApplicationDbContext(options);
        _context.Database.EnsureCreated();

        SeedTestData();

        _options = new RecommendationOptions
        {
            GenreWeight = 0.40,
            CategoryWeight = 0.20,
            YearWeight = 0.10,
            UserPreferenceWeight = 0.20,
            RatingWeight = 0.10,
            WatchHistoryWeight = 1.0,
            FavoriteWeight = 2.0,
            RatingHighWeight = 2.0,
            RatingMediumWeight = 1.0,
            RatingLowWeight = -1.0
        };

        var iOptions = Options.Create(_options);
        _scorer = new RecommendationScorer(iOptions);
        _recommendationService = new RecommendationService(_context, _scorer, iOptions);
    }

    private void SeedTestData()
    {
        // Clean default seed data
        _context.EpisodeSources.RemoveRange(_context.EpisodeSources);
        _context.Episodes.RemoveRange(_context.Episodes);
        _context.WatchHistories.RemoveRange(_context.WatchHistories);
        _context.Favorites.RemoveRange(_context.Favorites);
        _context.MovieComments.RemoveRange(_context.MovieComments);
        _context.MovieRatings.RemoveRange(_context.MovieRatings);
        _context.MovieGenres.RemoveRange(_context.MovieGenres);
        _context.Movies.RemoveRange(_context.Movies);
        _context.Genres.RemoveRange(_context.Genres);
        _context.Categories.RemoveRange(_context.Categories);
        _context.Users.RemoveRange(_context.Users);
        _context.SaveChanges();

        // 1. Users
        var user1 = new User { Id = 1, Username = "alice", Email = "alice@example.com", PasswordHash = "h1", RoleId = 1, IsActive = true };
        var user2 = new User { Id = 2, Username = "bob", Email = "bob@example.com", PasswordHash = "h2", RoleId = 1, IsActive = true };
        var userNew = new User { Id = 3, Username = "newbie", Email = "newbie@example.com", PasswordHash = "h3", RoleId = 1, IsActive = true };
        _context.Users.AddRange(user1, user2, userNew);

        // 2. Genres
        var action = new Genre { Id = 1, Name = "Action", Slug = "action" };
        var crime = new Genre { Id = 2, Name = "Crime", Slug = "crime" };
        var scifi = new Genre { Id = 3, Name = "Sci-Fi", Slug = "sci-fi" };
        var comedy = new Genre { Id = 4, Name = "Comedy", Slug = "comedy" };
        var horror = new Genre { Id = 5, Name = "Horror", Slug = "horror" };
        _context.Genres.AddRange(action, crime, scifi, comedy, horror);

        // 3. Movies
        // Movie 10: Action + Crime (Release 2024, Series, Rating 9.0) - Target
        var m10 = new Movie
        {
            Id = 10,
            Title = "Cyber Heist",
            Slug = "cyber-heist",
            ReleaseYear = 2024,
            MovieType = "series",
            Rating = 9.0m,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        // Movie 20: Action + Crime (Release 2024, Series, Rating 8.5) - Very similar to m10
        var m20 = new Movie
        {
            Id = 20,
            Title = "Digital Fugitive",
            Slug = "digital-fugitive",
            ReleaseYear = 2024,
            MovieType = "series",
            Rating = 8.5m,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };

        // Movie 30: Action only (Release 2020, Movie, Rating 7.0) - Partially similar to m10
        var m30 = new Movie
        {
            Id = 30,
            Title = "Fast Pursuit",
            Slug = "fast-pursuit",
            ReleaseYear = 2020,
            MovieType = "single",
            Rating = 7.0m,
            CreatedAt = DateTime.UtcNow.AddDays(-6)
        };

        // Movie 40: Comedy only (Release 2015, Movie, Rating 6.0) - Unrelated to m10
        var m40 = new Movie
        {
            Id = 40,
            Title = "Laugh Out Loud",
            Slug = "laugh-out-loud",
            ReleaseYear = 2015,
            MovieType = "single",
            Rating = 6.0m,
            CreatedAt = DateTime.UtcNow.AddDays(-4)
        };

        // Movie 50: Horror only (Release 2023, Movie, Rating 4.0) - Low rated
        var m50 = new Movie
        {
            Id = 50,
            Title = "Creepy House",
            Slug = "creepy-house",
            ReleaseYear = 2023,
            MovieType = "single",
            Rating = 4.0m,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };

        // Movie 60: Sci-Fi + Action (Release 2025, Series, Rating 9.5)
        var m60 = new Movie
        {
            Id = 60,
            Title = "Solar Orbit",
            Slug = "solar-orbit",
            ReleaseYear = 2025,
            MovieType = "series",
            Rating = 9.5m,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Movies.AddRange(m10, m20, m30, m40, m50, m60);

        // 4. Movie Genres
        _context.MovieGenres.AddRange(
            new MovieGenre { MovieId = 10, GenreId = 1 }, // m10: Action
            new MovieGenre { MovieId = 10, GenreId = 2 }, // m10: Crime
            new MovieGenre { MovieId = 20, GenreId = 1 }, // m20: Action
            new MovieGenre { MovieId = 20, GenreId = 2 }, // m20: Crime
            new MovieGenre { MovieId = 30, GenreId = 1 }, // m30: Action
            new MovieGenre { MovieId = 40, GenreId = 4 }, // m40: Comedy
            new MovieGenre { MovieId = 50, GenreId = 5 }, // m50: Horror
            new MovieGenre { MovieId = 60, GenreId = 1 }, // m60: Action
            new MovieGenre { MovieId = 60, GenreId = 3 }  // m60: Sci-Fi
        );

        // 5. Episodes
        _context.Episodes.Add(new Episode
        {
            Id = 1,
            MovieId = 30,
            EpisodeNumber = 1,
            Title = "Episode 1",
            Slug = "episode-1"
        });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    #region 1. RecommendationScorer Unit Tests

    [Fact]
    public void CalculateGenreSimilarity_IdenticalGenres_ReturnsOne()
    {
        var target = new[] { 1, 2, 3 };
        var candidate = new[] { 1, 2, 3 };

        var sim = _scorer.CalculateGenreSimilarity(target, candidate);
        Assert.Equal(1.0, sim);
    }

    [Fact]
    public void CalculateGenreSimilarity_DisjointGenres_ReturnsZero()
    {
        var target = new[] { 1, 2 };
        var candidate = new[] { 3, 4 };

        var sim = _scorer.CalculateGenreSimilarity(target, candidate);
        Assert.Equal(0.0, sim);
    }

    [Fact]
    public void CalculateGenreSimilarity_PartialOverlap_ReturnsAccurateJaccard()
    {
        var target = new[] { 1, 2, 3 };
        var candidate = new[] { 2, 3, 4 }; // common: 2, union: 4 -> 0.5

        var sim = _scorer.CalculateGenreSimilarity(target, candidate);
        Assert.Equal(0.5, sim);
    }

    [Fact]
    public void CalculateCategoryMatch_MatchingType_ReturnsOne()
    {
        var match = _scorer.CalculateCategoryMatch("series", "SERIES");
        Assert.Equal(1.0, match);
    }

    [Fact]
    public void CalculateCategoryMatch_DifferentType_ReturnsZero()
    {
        var match = _scorer.CalculateCategoryMatch("series", "single");
        Assert.Equal(0.0, match);
    }

    [Fact]
    public void CalculateYearProximity_SameYear_ReturnsOne()
    {
        var prox = _scorer.CalculateYearProximity(2024, 2024);
        Assert.Equal(1.0, prox);
    }

    [Fact]
    public void CalculateYearProximity_NearbyYear_ReturnsHighProximity()
    {
        var prox2Years = _scorer.CalculateYearProximity(2024, 2022);
        var prox15Years = _scorer.CalculateYearProximity(2024, 2005);

        Assert.Equal(0.8, prox2Years);
        Assert.Equal(0.0, prox15Years);
    }

    [Fact]
    public void CalculateRatingSignal_ValidRating_ReturnsNormalizedValue()
    {
        Assert.Equal(0.85, _scorer.CalculateRatingSignal(8.5m));
        Assert.Equal(1.0, _scorer.CalculateRatingSignal(10.0m));
        Assert.Equal(0.0, _scorer.CalculateRatingSignal(null));
        Assert.Equal(0.0, _scorer.CalculateRatingSignal(0m));
    }

    #endregion

    #region 2. Similar Movies Tests

    [Fact]
    public async Task GetSimilarMoviesAsync_MatchingGenresAndType_ScoresHigherThanUnrelated()
    {
        var result = await _recommendationService.GetSimilarMoviesAsync(10, new RecommendationQueryParameters { Page = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);

        // Target movie 10 itself must be strictly excluded
        Assert.DoesNotContain(result.Items, m => m.MovieId == 10);

        // Movie 20 (exact same genres and type) should be ranked higher than Movie 40 (Comedy)
        var movie20 = result.Items.FirstOrDefault(m => m.MovieId == 20);
        var movie40 = result.Items.FirstOrDefault(m => m.MovieId == 40);

        Assert.NotNull(movie20);
        Assert.NotNull(movie40);
        Assert.True(movie20.Score > movie40.Score, $"Expected movie20 score ({movie20.Score}) > movie40 score ({movie40.Score})");
        Assert.Contains("Action", movie20.Reason);
    }

    [Fact]
    public async Task GetSimilarMoviesAsync_NonExistentMovie_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _recommendationService.GetSimilarMoviesAsync(9999, new RecommendationQueryParameters()));
    }

    [Fact]
    public async Task GetSimilarMoviesAsync_Pagination_ReturnsCorrectSlice()
    {
        var page1 = await _recommendationService.GetSimilarMoviesAsync(10, new RecommendationQueryParameters { Page = 1, PageSize = 2 });
        var page2 = await _recommendationService.GetSimilarMoviesAsync(10, new RecommendationQueryParameters { Page = 2, PageSize = 2 });

        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page2.Page);
        Assert.NotEqual(page1.Items[0].MovieId, page2.Items[0].MovieId);
    }

    [Fact]
    public async Task GetSimilarMoviesAsync_InvalidPagination_ThrowsValidationException()
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _recommendationService.GetSimilarMoviesAsync(10, new RecommendationQueryParameters { Page = 0 }));

        await Assert.ThrowsAsync<ValidationException>(() =>
            _recommendationService.GetSimilarMoviesAsync(10, new RecommendationQueryParameters { Page = 1, PageSize = 150 }));
    }

    #endregion

    #region 3. Personalized Recommendations & Cold Start Tests

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_ColdStartNewUser_ReturnsPublicFallback()
    {
        // User 3 has no watch history, favorites, or ratings
        var result = await _recommendationService.GetPersonalizedRecommendationsAsync(3, new RecommendationQueryParameters());

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        // Fallback returns movies sorted by rating / popularity
        Assert.Equal(60, result.Items[0].MovieId); // Solar Orbit rating 9.5
        Assert.Contains("SHIN movie", result.Items[0].Reason);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_WatchHistoryInfluencesRecommendations()
    {
        // User 1 watched Movie 30 (Action)
        _context.WatchHistories.Add(new WatchHistory
        {
            UserId = 1,
            MovieId = 30,
            EpisodeId = 1,
            ProgressSeconds = 1200,
            DurationSeconds = 1200,
            LastWatchedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _recommendationService.GetPersonalizedRecommendationsAsync(
            1,
            new RecommendationQueryParameters { ExcludeConsumed = true });

        // Watched Movie 30 must be excluded when ExcludeConsumed is true
        Assert.DoesNotContain(result.Items, m => m.MovieId == 30);

        // Action movies (like 60, 10, 20) should score higher than Comedy (40) and Horror (50)
        var actionMovie = result.Items.FirstOrDefault(m => m.MovieId == 60 || m.MovieId == 20);
        var comedyMovie = result.Items.FirstOrDefault(m => m.MovieId == 40);

        Assert.NotNull(actionMovie);
        Assert.NotNull(comedyMovie);
        Assert.True(actionMovie.Score > comedyMovie.Score);
        Assert.Contains("Action", actionMovie.Reason);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_FavoritesProvideStrongPreferenceSignal()
    {
        // User 2 favorited Movie 40 (Comedy)
        _context.Favorites.Add(new Favorite
        {
            UserId = 2,
            MovieId = 40,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var result = await _recommendationService.GetPersonalizedRecommendationsAsync(
            2,
            new RecommendationQueryParameters { ExcludeConsumed = false });

        // Movie 40 (Comedy) receives strong positive preference from favorite
        var movie40 = result.Items.FirstOrDefault(m => m.MovieId == 40);
        Assert.NotNull(movie40);
        Assert.Contains("Comedy", movie40.Reason);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_HighRatingBoostsAndLowRatingPenalizes()
    {
        // User 1 rates Movie 60 (Sci-Fi) = 10 (boost), and Movie 50 (Horror) = 2 (penalty)
        _context.MovieRatings.AddRange(
            new MovieRating { UserId = 1, MovieId = 60, Rating = 10, CreatedAt = DateTime.UtcNow },
            new MovieRating { UserId = 1, MovieId = 50, Rating = 2, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var result = await _recommendationService.GetPersonalizedRecommendationsAsync(
            1,
            new RecommendationQueryParameters { ExcludeConsumed = false });

        var scifiMovie = result.Items.FirstOrDefault(m => m.MovieId == 60);
        var horrorMovie = result.Items.FirstOrDefault(m => m.MovieId == 50);

        Assert.NotNull(scifiMovie);
        Assert.NotNull(horrorMovie);
        Assert.True(scifiMovie.Score > horrorMovie.Score);
    }

    [Fact]
    public async Task GetPersonalizedRecommendationsAsync_NonExistentUser_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _recommendationService.GetPersonalizedRecommendationsAsync(8888, new RecommendationQueryParameters()));
    }

    #endregion

    #region 4. Public Recommendations Tests

    [Fact]
    public async Task GetPublicRecommendationsAsync_ReturnsHighestRatedFirst()
    {
        var result = await _recommendationService.GetPublicRecommendationsAsync(new RecommendationQueryParameters { Page = 1, PageSize = 10 });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Items);
        // Top rated is Movie 60 (Rating 9.5) followed by Movie 10 (Rating 9.0)
        Assert.Equal(60, result.Items[0].MovieId);
        Assert.Equal(10, result.Items[1].MovieId);
        Assert.True(result.Items[0].Rating >= result.Items[1].Rating);
    }

    #endregion

    #region 5. Controller & Authorization Tests

    [Fact]
    public void RecommendationsController_PersonalizedEndpoint_RequiresAuthorizeAttribute()
    {
        var method = typeof(RecommendationsController).GetMethod(nameof(RecommendationsController.GetPersonalizedRecommendations));
        Assert.NotNull(method);

        // Check controller-level or method-level Authorize
        var hasAuth = typeof(RecommendationsController).GetCustomAttribute<AuthorizeAttribute>() != null
                      || method.GetCustomAttribute<AuthorizeAttribute>() != null;
        Assert.True(hasAuth, "Personalized recommendations must require [Authorize].");
    }

    [Fact]
    public void RecommendationsController_PublicEndpoint_DoesNotRequireAuthorize()
    {
        var method = typeof(RecommendationsController).GetMethod(nameof(RecommendationsController.GetPublicRecommendations));
        Assert.NotNull(method);

        var hasAuthOnMethod = method.GetCustomAttribute<AuthorizeAttribute>() != null;
        Assert.False(hasAuthOnMethod, "Public recommendations must not require authorization.");
    }

    [Fact]
    public async Task RecommendationsController_Unauthenticated_ThrowsUnauthorizedException()
    {
        var mockCurrentUser = new FakeCurrentUserService(null, false);
        var controller = new RecommendationsController(_recommendationService, mockCurrentUser);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            controller.GetPersonalizedRecommendations(new RecommendationQueryParameters(), CancellationToken.None));
    }

    [Fact]
    public async Task RecommendationsController_AuthenticatedUser_CallsServiceWithUserFromJwt()
    {
        var mockCurrentUser = new FakeCurrentUserService(1, true);
        var controller = new RecommendationsController(_recommendationService, mockCurrentUser);

        var response = await controller.GetPersonalizedRecommendations(new RecommendationQueryParameters(), CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(response.Result);
        var pagedResult = Assert.IsType<PagedResult<RecommendationMovieDto>>(okResult.Value);

        Assert.NotNull(pagedResult);
    }

    #endregion

    private class FakeCurrentUserService : ICurrentUserService
    {
        private readonly int? _userId;
        public FakeCurrentUserService(int? userId, bool isAuthenticated)
        {
            _userId = userId;
            IsAuthenticated = isAuthenticated;
        }

        public int UserId => _userId ?? throw new UnauthorizedException("User is not authenticated.");
        public int? TryGetUserId() => _userId;
        public bool IsAuthenticated { get; }
        public string? Username => "testuser";
        public string? Role => "User";
    }
}
