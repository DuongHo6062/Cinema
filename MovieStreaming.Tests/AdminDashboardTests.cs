using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MovieStreaming.Controllers.Admin;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Tests;

public class AdminDashboardTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly AdminDashboardService _dashboardService;
    private readonly AdminMovieService _movieService;
    private readonly AdminGenreService _genreService;
    private readonly AdminCategoryService _categoryService;
    private readonly AdminEpisodeService _episodeService;
    private readonly AdminUserService _userService;
    private readonly AdminModerationService _moderationService;

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

    public AdminDashboardTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestApplicationDbContext(options);
        _context.Database.EnsureCreated();

        SeedTestData();

        _dashboardService = new AdminDashboardService(_context);
        _movieService = new AdminMovieService(_context);
        _genreService = new AdminGenreService(_context);
        _categoryService = new AdminCategoryService(_context);
        _episodeService = new AdminEpisodeService(_context);
        _userService = new AdminUserService(_context);
        _moderationService = new AdminModerationService(_context);
    }

    private void SeedTestData()
    {
        // Clean default seed data to avoid unique constraint violations
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

        // 1. Users: 1 Admin, 1 User
        var adminUser = new User
        {
            Id = 1,
            Username = "superadmin",
            Email = "admin@example.com",
            PasswordHash = "h1",
            RoleId = 2,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var regularUser = new User
        {
            Id = 2,
            Username = "regularuser",
            Email = "user@example.com",
            PasswordHash = "h2",
            RoleId = 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };

        _context.Users.AddRange(adminUser, regularUser);

        // 2. Genres
        var genre1 = new Genre { Id = 10, Name = "Action", Slug = "action", Description = "Action films" };
        var genre2 = new Genre { Id = 20, Name = "Drama", Slug = "drama", Description = "Drama films" };
        _context.Genres.AddRange(genre1, genre2);

        // 2b. Categories
        var cat1 = new Category { Id = 1, Name = "Trending", Slug = "trending", Description = "Trending movies" };
        var cat2 = new Category { Id = 2, Name = "Popular", Slug = "popular", Description = "Popular movies" };
        var cat3 = new Category { Id = 3, Name = "Top Rated", Slug = "top-rated", Description = "Top rated movies" };
        var cat4 = new Category { Id = 4, Name = "Latest", Slug = "latest", Description = "Latest movies" };
        _context.Categories.AddRange(cat1, cat2, cat3, cat4);

        // 3. Movies
        var movie1 = new Movie
        {
            Id = 100,
            Title = "Admin Test Movie 1",
            Slug = "admin-test-movie-1",
            ReleaseYear = 2024,
            Status = "completed",
            Rating = 8.5m,
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var movie2 = new Movie
        {
            Id = 200,
            Title = "Admin Test Movie 2",
            Slug = "admin-test-movie-2",
            ReleaseYear = 2025,
            Status = "ongoing",
            Rating = 9.0m,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _context.Movies.AddRange(movie1, movie2);

        // 4. Movie Genres
        _context.MovieGenres.Add(new MovieGenre { MovieId = 100, GenreId = 10 });

        // 5. Episodes & Sources
        var ep1 = new Episode
        {
            Id = 1001,
            MovieId = 100,
            EpisodeNumber = 1,
            SeasonNumber = 1,
            Title = "Tập 01",
            Slug = "tap-01",
            CreatedAt = DateTime.UtcNow
        };
        _context.Episodes.Add(ep1);

        var src1 = new EpisodeSource
        {
            Id = 5001,
            EpisodeId = 1001,
            ServerName = "Vietsub",
            VideoUrl = "https://example.com/video1.m3u8",
            CreatedAt = DateTime.UtcNow
        };
        _context.EpisodeSources.Add(src1);

        // 6. Comments
        var comment1 = new MovieComment
        {
            Id = 701,
            MovieId = 100,
            UserId = 2,
            Content = "Test comment by regular user",
            CreatedAt = DateTime.UtcNow
        };
        _context.MovieComments.Add(comment1);

        // 7. Ratings
        var rating1 = new MovieRating
        {
            Id = 801,
            MovieId = 100,
            UserId = 2,
            Rating = 8,
            CreatedAt = DateTime.UtcNow
        };
        _context.MovieRatings.Add(rating1);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    #region 1. Admin Authorization Attributes

    [Theory]
    [InlineData(typeof(AdminDashboardController))]
    [InlineData(typeof(AdminMoviesController))]
    [InlineData(typeof(AdminGenresController))]
    [InlineData(typeof(AdminCategoriesController))]
    [InlineData(typeof(AdminEpisodesController))]
    [InlineData(typeof(AdminEpisodeSourcesController))]
    [InlineData(typeof(AdminUsersController))]
    [InlineData(typeof(AdminCommentsController))]
    [InlineData(typeof(AdminRatingsController))]
    [InlineData(typeof(AdminImportController))]
    public void AdminControllers_RequireAdminRole(Type controllerType)
    {
        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Admin", authAttr.Roles);
    }

    #endregion

    #region 2. Movie Management Tests

    [Fact]
    public async Task AdminMovies_CreateMovie_CreatesSuccessfullyWithGenres()
    {
        var request = new AdminCreateMovieRequest
        {
            Title = "New Epic Movie",
            ReleaseYear = 2026,
            Status = "ongoing",
            MovieType = "series",
            GenreIds = new List<int> { 10, 20 }
        };

        var result = await _movieService.CreateMovieAsync(request);

        Assert.NotNull(result);
        Assert.Equal("New Epic Movie", result.Title);
        Assert.Equal("new-epic-movie", result.Slug);
        Assert.Equal(2, result.Genres.Count);

        var dbMovie = await _context.Movies.FindAsync(result.Id);
        Assert.NotNull(dbMovie);
    }

    [Fact]
    public async Task AdminMovies_CreateMovie_DuplicateSlug_ThrowsConflictException()
    {
        var request = new AdminCreateMovieRequest
        {
            Title = "Duplicate Slug",
            Slug = "admin-test-movie-1"
        };

        await Assert.ThrowsAsync<ConflictException>(() => _movieService.CreateMovieAsync(request));
    }

    [Fact]
    public async Task AdminMovies_UpdateMovie_UpdatesFieldsSuccessfully()
    {
        var request = new AdminUpdateMovieRequest
        {
            Title = "Updated Title",
            Status = "completed",
            Rating = 9.5m,
            GenreIds = new List<int> { 20 }
        };

        var result = await _movieService.UpdateMovieAsync(100, request);

        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("completed", result.Status);
        Assert.Equal(9.5m, result.Rating);
        Assert.Single(result.Genres);
        Assert.Equal("Drama", result.Genres[0].Name);
    }

    [Fact]
    public async Task AdminMovies_DeleteMovie_SafelyDeletesChildEpisodesAndSources()
    {
        var deleted = await _movieService.DeleteMovieAsync(100);
        Assert.True(deleted);

        var movie = await _context.Movies.FindAsync(100);
        Assert.Null(movie);

        // Child episodes and sources must be removed
        var episode = await _context.Episodes.FindAsync(1001);
        Assert.Null(episode);

        var source = await _context.EpisodeSources.FindAsync(5001);
        Assert.Null(source);
    }

    [Fact]
    public async Task AdminMovies_GetMovies_PaginationAndFilteringWork()
    {
        var paged = await _movieService.GetMoviesAsync(search: "Test", genreId: 10, year: 2024, status: "completed", sortBy: "newest", page: 1, pageSize: 10);

        Assert.Equal(1, paged.TotalItems);
        Assert.Single(paged.Items);
        Assert.Equal(100, paged.Items[0].Id);
    }

    #endregion

    #region 3. Genre Management Tests

    [Fact]
    public async Task AdminGenres_CreateGenre_CreatesSuccessfully()
    {
        var request = new AdminCreateGenreRequest
        {
            Name = "Comedy",
            Description = "Funny movies"
        };

        var result = await _genreService.CreateGenreAsync(request);

        Assert.Equal("Comedy", result.Name);
        Assert.Equal("comedy", result.Slug);

        var count = await _context.Genres.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task AdminGenres_CreateGenre_DuplicateName_ThrowsConflictException()
    {
        var request = new AdminCreateGenreRequest { Name = "Action" };
        await Assert.ThrowsAsync<ConflictException>(() => _genreService.CreateGenreAsync(request));
    }

    [Fact]
    public async Task AdminGenres_UpdateGenre_UpdatesSuccessfully()
    {
        var request = new AdminUpdateGenreRequest
        {
            Name = "Action Thriller",
            Description = "Action packed"
        };

        var result = await _genreService.UpdateGenreAsync(10, request);

        Assert.Equal("Action Thriller", result.Name);
        Assert.Equal("action-thriller", result.Slug);
    }

    [Fact]
    public async Task AdminGenres_DeleteGenre_RemovesGenreAndMovieGenreMappings()
    {
        var deleted = await _genreService.DeleteGenreAsync(10);
        Assert.True(deleted);

        var genre = await _context.Genres.FindAsync(10);
        Assert.Null(genre);

        var mapping = await _context.MovieGenres.AnyAsync(mg => mg.GenreId == 10);
        Assert.False(mapping);
    }

    #endregion

    #region 4. Category Management Tests

    [Fact]
    public async Task AdminCategories_CreateCategory_CreatesSuccessfully()
    {
        var request = new AdminCreateCategoryRequest
        {
            Name = "Blockbuster",
            Description = "Big budget hits"
        };

        var result = await _categoryService.CreateCategoryAsync(request);

        Assert.Equal("Blockbuster", result.Name);
        Assert.Equal("blockbuster", result.Slug);
    }

    [Fact]
    public async Task AdminCategories_UpdateCategory_UpdatesSuccessfully()
    {
        var request = new AdminUpdateCategoryRequest
        {
            Name = "Top Trending",
            Description = "Top of the charts"
        };

        var result = await _categoryService.UpdateCategoryAsync(1, request); // Category 1 is 'Trending'
        Assert.Equal("Top Trending", result.Name);
    }

    [Fact]
    public async Task AdminCategories_DeleteCategory_DeletesSuccessfully()
    {
        var deleted = await _categoryService.DeleteCategoryAsync(4); // Category 4 is 'Latest'
        Assert.True(deleted);

        var cat = await _context.Categories.FindAsync(4);
        Assert.Null(cat);
    }

    #endregion

    #region 5. Episode & Source Management Tests

    [Fact]
    public async Task AdminEpisodes_CreateEpisode_CreatesSuccessfully()
    {
        var request = new AdminCreateEpisodeRequest
        {
            EpisodeNumber = 2,
            Title = "Tập 02",
            Duration = 45
        };

        var result = await _episodeService.CreateEpisodeAsync(100, request);

        Assert.Equal(2, result.EpisodeNumber);
        Assert.Equal(100, result.MovieId);
        Assert.Equal("Tập 02", result.Title);
    }

    [Fact]
    public async Task AdminEpisodes_CreateEpisode_DuplicateNumber_ThrowsConflictException()
    {
        var request = new AdminCreateEpisodeRequest
        {
            EpisodeNumber = 1 // Episode 1 already seeded
        };

        await Assert.ThrowsAsync<ConflictException>(() => _episodeService.CreateEpisodeAsync(100, request));
    }

    [Fact]
    public async Task AdminEpisodes_UpdateEpisode_UpdatesSuccessfully()
    {
        var request = new AdminUpdateEpisodeRequest
        {
            EpisodeNumber = 1,
            Title = "Pilot Episode",
            Duration = 50
        };

        var result = await _episodeService.UpdateEpisodeAsync(1001, request);
        Assert.Equal("Pilot Episode", result.Title);
        Assert.Equal(50, result.Duration);
    }

    [Fact]
    public async Task AdminEpisodes_DeleteEpisode_SafelyDeletesEpisodeAndSources()
    {
        var deleted = await _episodeService.DeleteEpisodeAsync(1001);
        Assert.True(deleted);

        var ep = await _context.Episodes.FindAsync(1001);
        Assert.Null(ep);

        var src = await _context.EpisodeSources.FindAsync(5001);
        Assert.Null(src);
    }

    [Fact]
    public async Task AdminEpisodeSources_CreateUpdateDelete_WorksSuccessfully()
    {
        // 1. Create Source
        var createReq = new AdminCreateEpisodeSourceRequest
        {
            ServerName = "Backup Server",
            VideoUrl = "https://example.com/backup.m3u8",
            SourceType = "HLS"
        };
        var source = await _episodeService.CreateSourceAsync(1001, createReq);
        Assert.Equal("Backup Server", source.ServerName);

        // 2. Update Source
        var updateReq = new AdminUpdateEpisodeSourceRequest
        {
            ServerName = "Primary Fast Server",
            VideoUrl = "https://example.com/fast.m3u8"
        };
        var updated = await _episodeService.UpdateSourceAsync(source.Id, updateReq);
        Assert.Equal("Primary Fast Server", updated.ServerName);

        // 3. Delete Source
        var deleted = await _episodeService.DeleteSourceAsync(source.Id);
        Assert.True(deleted);

        var dbSource = await _context.EpisodeSources.FindAsync(source.Id);
        Assert.Null(dbSource);
    }

    #endregion

    #region 6. User Management & Last Admin Protection

    [Fact]
    public async Task AdminUsers_ListUsers_ReturnsCorrectInformationWithoutSensitiveData()
    {
        var paged = await _userService.GetUsersAsync(search: "admin", roleId: 2, isActive: true, page: 1, pageSize: 10);

        Assert.Equal(1, paged.TotalItems);
        Assert.Equal("superadmin", paged.Items[0].Username);
        Assert.Equal("Admin", paged.Items[0].RoleName);
    }

    [Fact]
    public async Task AdminUsers_DeactivateRegularUser_Succeeds()
    {
        var result = await _userService.UpdateUserStatusAsync(2, isActive: false);

        Assert.False(result.IsActive);

        var dbUser = await _context.Users.FindAsync(2);
        Assert.False(dbUser!.IsActive);
    }

    [Fact]
    public async Task AdminUsers_DeactivateLastAdmin_ThrowsValidationException()
    {
        // User 1 is the only active Admin
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.UpdateUserStatusAsync(1, isActive: false));
    }

    [Fact]
    public async Task AdminUsers_ChangeLastAdminRole_ThrowsValidationException()
    {
        // User 1 is the only active Admin, changing role away from 2 (Admin) to 1 (User) must fail
        await Assert.ThrowsAsync<ValidationException>(() =>
            _userService.UpdateUserRoleAsync(1, roleId: 1));
    }

    [Fact]
    public async Task AdminUsers_WhenMultipleAdminsExist_DeactivatingOneSucceeds()
    {
        // Create second active Admin
        var admin2 = new User
        {
            Id = 3,
            Username = "secondadmin",
            Email = "admin2@example.com",
            PasswordHash = "h3",
            RoleId = 2,
            IsActive = true
        };
        _context.Users.Add(admin2);
        await _context.SaveChangesAsync();

        // Now deactivating User 1 should succeed
        var result = await _userService.UpdateUserStatusAsync(1, isActive: false);
        Assert.False(result.IsActive);
    }

    #endregion

    #region 7. Moderation Tests

    [Fact]
    public async Task AdminModeration_GetComments_ReturnsAllComments()
    {
        var paged = await _moderationService.GetCommentsAsync(movieId: null, userId: null, search: null, page: 1, pageSize: 10);

        Assert.Equal(1, paged.TotalItems);
        Assert.Equal("Test comment by regular user", paged.Items[0].Content);
        Assert.Equal("Admin Test Movie 1", paged.Items[0].MovieTitle);
        Assert.Equal("regularuser", paged.Items[0].Username);
    }

    [Fact]
    public async Task AdminModeration_DeleteComment_AdminCanDeleteAnyComment()
    {
        var deleted = await _moderationService.DeleteCommentAsync(701);
        Assert.True(deleted);

        var dbComment = await _context.MovieComments.FindAsync(701);
        Assert.Null(dbComment);
    }

    [Fact]
    public async Task AdminModeration_GetRatings_ReturnsAllRatings()
    {
        var paged = await _moderationService.GetRatingsAsync(movieId: null, userId: null, page: 1, pageSize: 10);

        Assert.Equal(1, paged.TotalItems);
        Assert.Equal(8, paged.Items[0].Rating);
        Assert.Equal("Admin Test Movie 1", paged.Items[0].MovieTitle);
    }

    #endregion

    #region 8. Dashboard Statistics & Recent Activity

    [Fact]
    public async Task AdminDashboard_GetStatistics_ReturnsAccurateCounts()
    {
        var stats = await _dashboardService.GetStatisticsAsync();

        Assert.NotNull(stats);
        Assert.True(stats.TotalMovies >= 2);
        Assert.True(stats.TotalUsers >= 2);
        Assert.True(stats.TotalGenres >= 2);
        Assert.True(stats.TotalCategories >= 4);
        Assert.True(stats.TotalEpisodes >= 1);
        Assert.True(stats.TotalComments >= 1);
        Assert.True(stats.TotalRatings >= 1);
    }

    [Fact]
    public async Task AdminDashboard_GetRecentActivity_ReturnsRecentData()
    {
        var activity = await _dashboardService.GetRecentActivityAsync();

        Assert.NotNull(activity);
        Assert.NotEmpty(activity.RecentUsers);
        Assert.NotEmpty(activity.RecentMovies);
        Assert.NotEmpty(activity.RecentComments);
        Assert.NotEmpty(activity.RecentRatings);
    }

    #endregion
}
