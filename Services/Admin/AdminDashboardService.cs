using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;

    public AdminDashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminDashboardDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var totalMovies = await _context.Movies.CountAsync(cancellationToken);
        var totalUsers = await _context.Users.CountAsync(cancellationToken);
        var totalGenres = await _context.Genres.CountAsync(cancellationToken);
        var totalCategories = await _context.Categories.CountAsync(cancellationToken);
        var totalEpisodes = await _context.Episodes.CountAsync(cancellationToken);
        var totalComments = await _context.MovieComments.CountAsync(cancellationToken);
        var totalRatings = await _context.MovieRatings.CountAsync(cancellationToken);
        var totalFavorites = await _context.Favorites.CountAsync(cancellationToken);
        var totalWatchHistory = await _context.WatchHistories.CountAsync(cancellationToken);

        return new AdminDashboardDto
        {
            TotalMovies = totalMovies,
            TotalUsers = totalUsers,
            TotalGenres = totalGenres,
            TotalCategories = totalCategories,
            TotalEpisodes = totalEpisodes,
            TotalComments = totalComments,
            TotalRatings = totalRatings,
            TotalFavorites = totalFavorites,
            TotalWatchHistory = totalWatchHistory
        };
    }

    public async Task<AdminRecentActivityDto> GetRecentActivityAsync(CancellationToken cancellationToken = default)
    {
        var recentUsers = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .Select(u => new AdminRecentUserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                RoleName = u.Role != null ? u.Role.Name : "User",
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentMovies = await _context.Movies
            .AsNoTracking()
            .OrderByDescending(m => m.CreatedAt)
            .Take(5)
            .Select(m => new AdminRecentMovieDto
            {
                Id = m.Id,
                Title = m.Title,
                Slug = m.Slug,
                PosterUrl = m.PosterUrl,
                Status = m.Status,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentComments = await _context.MovieComments
            .AsNoTracking()
            .Include(c => c.Movie)
            .Include(c => c.User)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new AdminRecentCommentDto
            {
                Id = c.Id,
                MovieId = c.MovieId,
                MovieTitle = c.Movie != null ? c.Movie.Title : string.Empty,
                UserId = c.UserId,
                Username = c.User != null ? c.User.Username : "User",
                Content = c.Content,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var recentRatings = await _context.MovieRatings
            .AsNoTracking()
            .Include(r => r.Movie)
            .Include(r => r.User)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new AdminRecentRatingDto
            {
                Id = r.Id,
                MovieId = r.MovieId,
                MovieTitle = r.Movie != null ? r.Movie.Title : string.Empty,
                UserId = r.UserId,
                Username = r.User != null ? r.User.Username : "User",
                Rating = r.Rating,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new AdminRecentActivityDto
        {
            RecentUsers = recentUsers,
            RecentMovies = recentMovies,
            RecentComments = recentComments,
            RecentRatings = recentRatings
        };
    }
}
