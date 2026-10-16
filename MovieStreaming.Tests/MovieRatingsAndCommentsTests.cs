using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieStreaming.Controllers;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Comments;
using MovieStreaming.DTOs.Ratings;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services.Authentication;
using MovieStreaming.Services.Comments;
using MovieStreaming.Services.Ratings;

namespace MovieStreaming.Tests;

public class MovieRatingsAndCommentsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly MovieRatingRepository _ratingRepository;
    private readonly MovieCommentRepository _commentRepository;
    private readonly MovieRepository _movieRepository;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly MovieRatingService _ratingService;
    private readonly MovieCommentService _commentService;
    private readonly MovieRatingsController _ratingsController;
    private readonly MovieCommentsController _movieCommentsController;
    private readonly CommentsController _commentsController;

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

    public MovieRatingsAndCommentsTests()
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
        _mockCurrentUserService.Setup(u => u.Username).Returns("alice");
        _mockCurrentUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(u => u.TryGetUserId()).Returns(1);

        _ratingRepository = new MovieRatingRepository(_context);
        _commentRepository = new MovieCommentRepository(_context);
        _movieRepository = new MovieRepository(_context);

        _ratingService = new MovieRatingService(_ratingRepository, _movieRepository, _mockCurrentUserService.Object);
        _commentService = new MovieCommentService(_commentRepository, _movieRepository, _mockCurrentUserService.Object);

        _ratingsController = new MovieRatingsController(_ratingService);
        _movieCommentsController = new MovieCommentsController(_commentService);
        _commentsController = new CommentsController(_commentService);
    }

    private void SeedTestData()
    {
        var user1 = new User { Id = 1, Username = "alice", Email = "alice@example.com", PasswordHash = "h1", RoleId = 1 };
        var user2 = new User { Id = 2, Username = "bob", Email = "bob@example.com", PasswordHash = "h2", RoleId = 1 };
        _context.Users.AddRange(user1, user2);

        var movie1 = new Movie
        {
            Id = 10,
            Title = "Test Movie 1",
            Slug = "test-movie-1",
            Rating = 8.8m, // External rating preserved
            ReleaseYear = 2010,
            CreatedAt = DateTime.UtcNow
        };

        var movie2 = new Movie
        {
            Id = 20,
            Title = "Test Movie 2",
            Slug = "test-movie-2",
            Rating = 8.6m, // External rating preserved
            ReleaseYear = 2014,
            CreatedAt = DateTime.UtcNow
        };

        _context.Movies.AddRange(movie1, movie2);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }

    #region Ratings Tests

    [Fact]
    public async Task RateMovie_NewRating_CreatesSuccessfully()
    {
        var result = await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 9 });

        Assert.NotNull(result);
        Assert.Equal(10, result.MovieId);
        Assert.Equal(1, result.UserId);
        Assert.Equal(9, result.Rating);

        var count = await _context.MovieRatings.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RateMovie_ExistingRating_UpdatesRatingAndTimestamp()
    {
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 7 });
        var updated = await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 10 });

        Assert.Equal(10, updated.Rating);
        Assert.NotNull(updated.UpdatedAt);

        var count = await _context.MovieRatings.CountAsync(r => r.UserId == 1 && r.MovieId == 10);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RateMovie_DuplicateCalls_DoesNotCreateDuplicateRecords()
    {
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 8 });
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 8 });

        var count = await _context.MovieRatings.CountAsync(r => r.UserId == 1 && r.MovieId == 10);
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public async Task RateMovie_RatingBelow1_ThrowsValidationException(int rating)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = rating }));
    }

    [Theory]
    [InlineData(11)]
    [InlineData(100)]
    public async Task RateMovie_RatingAbove10_ThrowsValidationException(int rating)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = rating }));
    }

    [Fact]
    public async Task RateMovie_NonExistentMovie_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _ratingService.RateMovieAsync(999, new CreateRatingRequest { Rating = 8 }));
    }

    [Fact]
    public async Task GetCurrentUserRating_WhenRated_ReturnsUserRating()
    {
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 8 });

        var result = await _ratingService.GetCurrentUserRatingAsync(10);
        Assert.Equal(10, result.MovieId);
        Assert.Equal(8, result.Rating);
    }

    [Fact]
    public async Task GetCurrentUserRating_WhenNotRated_ReturnsNullRating()
    {
        var result = await _ratingService.GetCurrentUserRatingAsync(10);
        Assert.Equal(10, result.MovieId);
        Assert.Null(result.Rating);
    }

    [Fact]
    public async Task DeleteRating_OwnRating_DeletesSuccessfully()
    {
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 9 });

        var deleted = await _ratingService.DeleteRatingAsync(10);
        Assert.True(deleted);

        var status = await _ratingService.GetCurrentUserRatingAsync(10);
        Assert.Null(status.Rating);
    }

    [Fact]
    public async Task DeleteRating_NotExisting_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _ratingService.DeleteRatingAsync(10));
    }

    [Fact]
    public async Task GetRatingSummary_CalculatesAverageAndTotalCorrectly()
    {
        // User 1 rates 8
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 8 });

        // User 2 rates 10
        _mockCurrentUserService.Setup(u => u.UserId).Returns(2);
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 10 });

        var summary = await _ratingService.GetRatingSummaryAsync(10);

        Assert.Equal(10, summary.MovieId);
        Assert.Equal(2, summary.TotalRatings);
        Assert.Equal(9.0m, summary.AverageRating);
        Assert.Equal(1, summary.Distribution["8"]);
        Assert.Equal(1, summary.Distribution["10"]);
        Assert.Equal(0, summary.Distribution["1"]);
    }

    [Fact]
    public async Task GetRatingSummary_MovieWithNoRatings_ReturnsZeroAverageAndZeroTotal()
    {
        var summary = await _ratingService.GetRatingSummaryAsync(20);

        Assert.Equal(20, summary.MovieId);
        Assert.Equal(0, summary.TotalRatings);
        Assert.Equal(0.0m, summary.AverageRating);
        Assert.Equal(10, summary.Distribution.Count);
        Assert.Equal(0, summary.Distribution["5"]);
    }

    [Fact]
    public async Task GetRatingSummary_NonExistentMovie_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _ratingService.GetRatingSummaryAsync(999));
    }

    [Fact]
    public async Task Rating_ClaimIsolation_UserCannotManipulateAnotherUserRating()
    {
        // User 1 rates 7
        _mockCurrentUserService.Setup(u => u.UserId).Returns(1);
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 7 });

        // User 2 rates 9
        _mockCurrentUserService.Setup(u => u.UserId).Returns(2);
        await _ratingService.RateMovieAsync(10, new CreateRatingRequest { Rating = 9 });

        // User 2 deletes their rating
        await _ratingService.DeleteRatingAsync(10);

        // User 1's rating must still exist
        _mockCurrentUserService.Setup(u => u.UserId).Returns(1);
        var user1Rating = await _ratingService.GetCurrentUserRatingAsync(10);
        Assert.Equal(7, user1Rating.Rating);
    }

    #endregion

    #region Comments Tests

    [Fact]
    public async Task AddComment_ValidContent_CreatesCommentSuccessfully()
    {
        var result = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "Great movie!" });

        Assert.NotNull(result);
        Assert.Equal(10, result.MovieId);
        Assert.Equal(1, result.UserId);
        Assert.Equal("alice", result.Username);
        Assert.Equal("Great movie!", result.Content);
        Assert.True(result.IsOwner);

        var count = await _context.MovieComments.CountAsync();
        Assert.Equal(1, count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task AddComment_EmptyOrWhitespaceContent_ThrowsValidationException(string? content)
    {
        await Assert.ThrowsAsync<ValidationException>(() =>
            _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = content! }));
    }

    [Fact]
    public async Task AddComment_ExceedingMaxLength_ThrowsValidationException()
    {
        var longContent = new string('A', 1001);
        await Assert.ThrowsAsync<ValidationException>(() =>
            _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = longContent }));
    }

    [Fact]
    public async Task AddComment_NonExistentMovie_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _commentService.AddCommentAsync(999, new CreateCommentRequest { Content = "Nice!" }));
    }

    [Fact]
    public async Task GetComments_ReturnsSortedByCreatedAtDescending()
    {
        var c1 = new MovieComment
        {
            UserId = 1,
            MovieId = 10,
            Content = "Old comment",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var c2 = new MovieComment
        {
            UserId = 2,
            MovieId = 10,
            Content = "New comment",
            CreatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        _context.MovieComments.AddRange(c1, c2);
        await _context.SaveChangesAsync();

        var result = await _commentService.GetMovieCommentsAsync(10);

        Assert.Equal(2, result.TotalItems);
        Assert.Equal("New comment", result.Items[0].Content);
        Assert.Equal("Old comment", result.Items[1].Content);
    }

    [Fact]
    public async Task GetComments_Pagination_ReturnsCorrectPageAndPageSize()
    {
        for (int i = 1; i <= 5; i++)
        {
            _context.MovieComments.Add(new MovieComment
            {
                UserId = 1,
                MovieId = 10,
                Content = $"Comment {i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await _context.SaveChangesAsync();

        var page1 = await _commentService.GetMovieCommentsAsync(10, page: 1, pageSize: 2);
        Assert.Equal(5, page1.TotalItems);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(2, page1.Items.Count);

        var page3 = await _commentService.GetMovieCommentsAsync(10, page: 3, pageSize: 2);
        Assert.Single(page3.Items);
    }

    [Fact]
    public async Task GetComments_IsOwnerFlag_CorrectlyIdentifiesCurrentUserComments()
    {
        var c1 = new MovieComment { UserId = 1, MovieId = 10, Content = "Alice's comment", CreatedAt = DateTime.UtcNow };
        var c2 = new MovieComment { UserId = 2, MovieId = 10, Content = "Bob's comment", CreatedAt = DateTime.UtcNow.AddSeconds(1) };
        _context.MovieComments.AddRange(c1, c2);
        await _context.SaveChangesAsync();

        // When viewed as Alice (UserId = 1)
        _mockCurrentUserService.Setup(u => u.TryGetUserId()).Returns(1);
        var resultAlice = await _commentService.GetMovieCommentsAsync(10);
        var aliceComment = resultAlice.Items.First(c => c.Content == "Alice's comment");
        var bobComment = resultAlice.Items.First(c => c.Content == "Bob's comment");

        Assert.True(aliceComment.IsOwner);
        Assert.False(bobComment.IsOwner);

        // When viewed anonymously (UserId = null)
        _mockCurrentUserService.Setup(u => u.TryGetUserId()).Returns((int?)null);
        var resultAnon = await _commentService.GetMovieCommentsAsync(10);
        Assert.All(resultAnon.Items, c => Assert.False(c.IsOwner));
    }

    [Fact]
    public async Task EditComment_OwnComment_UpdatesSuccessfully()
    {
        var comment = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "Initial" });

        var edited = await _commentService.EditCommentAsync(comment.Id, new UpdateCommentRequest { Content = "Updated content" });

        Assert.Equal("Updated content", edited.Content);
        Assert.NotNull(edited.UpdatedAt);

        var dbComment = await _context.MovieComments.FindAsync(comment.Id);
        Assert.Equal("Updated content", dbComment!.Content);
    }

    [Fact]
    public async Task EditComment_AnotherUserComment_ThrowsForbiddenException()
    {
        // Alice creates comment
        _mockCurrentUserService.Setup(u => u.UserId).Returns(1);
        var comment = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "Alice's post" });

        // Bob tries to edit Alice's comment
        _mockCurrentUserService.Setup(u => u.UserId).Returns(2);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.EditCommentAsync(comment.Id, new UpdateCommentRequest { Content = "Hacked by Bob" }));
    }

    [Fact]
    public async Task EditComment_NonExistentComment_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _commentService.EditCommentAsync(999, new UpdateCommentRequest { Content = "Edit" }));
    }

    [Fact]
    public async Task DeleteComment_OwnComment_DeletesSuccessfully()
    {
        var comment = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "To delete" });

        var deleted = await _commentService.DeleteCommentAsync(comment.Id);
        Assert.True(deleted);

        var dbComment = await _context.MovieComments.FindAsync(comment.Id);
        Assert.Null(dbComment);
    }

    [Fact]
    public async Task DeleteComment_AnotherUserComment_ThrowsForbiddenException()
    {
        // Alice creates comment
        _mockCurrentUserService.Setup(u => u.UserId).Returns(1);
        var comment = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "Alice's post" });

        // Bob tries to delete Alice's comment
        _mockCurrentUserService.Setup(u => u.UserId).Returns(2);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _commentService.DeleteCommentAsync(comment.Id));
    }

    [Fact]
    public async Task DeleteComment_NonExistentComment_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _commentService.DeleteCommentAsync(999));
    }

    #endregion

    #region Controller HTTP Response Tests

    [Fact]
    public async Task RatingsController_RateMovie_ReturnsOkWithRatingDto()
    {
        var actionResult = await _ratingsController.RateMovie(10, new CreateRatingRequest { Rating = 8 }, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<RatingDto>(okResult.Value);
        Assert.Equal(8, dto.Rating);
    }

    [Fact]
    public async Task MovieCommentsController_AddComment_ReturnsCreatedWithCommentDto()
    {
        var actionResult = await _movieCommentsController.AddComment(10, new CreateCommentRequest { Content = "Super film" }, CancellationToken.None);
        var createdResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(201, createdResult.StatusCode);
        var dto = Assert.IsType<CommentDto>(createdResult.Value);
        Assert.Equal("Super film", dto.Content);
    }

    [Fact]
    public async Task CommentsController_EditComment_ReturnsOkWithUpdatedCommentDto()
    {
        var comment = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "Draft" });
        var actionResult = await _commentsController.EditComment(comment.Id, new UpdateCommentRequest { Content = "Final" }, CancellationToken.None);
        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var dto = Assert.IsType<CommentDto>(okResult.Value);
        Assert.Equal("Final", dto.Content);
    }

    [Fact]
    public async Task CommentsController_DeleteComment_ReturnsOk()
    {
        var comment = await _commentService.AddCommentAsync(10, new CreateCommentRequest { Content = "Temp" });
        var actionResult = await _commentsController.DeleteComment(comment.Id, CancellationToken.None);
        Assert.IsType<OkObjectResult>(actionResult);
    }

    #endregion
}
