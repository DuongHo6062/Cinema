using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;

namespace MovieStreaming.Services.Admin;

public class AdminModerationService : IAdminModerationService
{
    private readonly ApplicationDbContext _context;

    public AdminModerationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminCommentDto>> GetCommentsAsync(
        int? movieId,
        int? userId,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var query = _context.MovieComments
            .AsNoTracking()
            .AsQueryable();

        if (movieId.HasValue)
        {
            query = query.Where(c => c.MovieId == movieId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(c => c.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c => c.Content.ToLower().Contains(searchLower));
        }

        query = query.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id);

        var totalItems = await query.CountAsync(cancellationToken);

        var comments = await query
            .Include(c => c.Movie)
            .Include(c => c.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = comments.Select(c => new AdminCommentDto
        {
            Id = c.Id,
            MovieId = c.MovieId,
            MovieTitle = c.Movie != null ? c.Movie.Title : string.Empty,
            MovieSlug = c.Movie != null ? c.Movie.Slug : string.Empty,
            UserId = c.UserId,
            Username = c.User != null ? c.User.Username : "User",
            UserEmail = c.User != null ? c.User.Email : string.Empty,
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt
        }).ToList();

        return new PagedResult<AdminCommentDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminCommentDto> GetCommentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Comment ID must be greater than 0.");
        }

        var comment = await _context.MovieComments
            .AsNoTracking()
            .Include(c => c.Movie)
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (comment == null)
        {
            throw new NotFoundException($"Comment with ID {id} not found.");
        }

        return new AdminCommentDto
        {
            Id = comment.Id,
            MovieId = comment.MovieId,
            MovieTitle = comment.Movie != null ? comment.Movie.Title : string.Empty,
            MovieSlug = comment.Movie != null ? comment.Movie.Slug : string.Empty,
            UserId = comment.UserId,
            Username = comment.User != null ? comment.User.Username : "User",
            UserEmail = comment.User != null ? comment.User.Email : string.Empty,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt
        };
    }

    public async Task<bool> DeleteCommentAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Comment ID must be greater than 0.");
        }

        var comment = await _context.MovieComments
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (comment == null)
        {
            throw new NotFoundException($"Comment with ID {id} not found.");
        }

        _context.MovieComments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<PagedResult<AdminRatingDto>> GetRatingsAsync(
        int? movieId,
        int? userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var query = _context.MovieRatings
            .AsNoTracking()
            .AsQueryable();

        if (movieId.HasValue)
        {
            query = query.Where(r => r.MovieId == movieId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(r => r.UserId == userId.Value);
        }

        query = query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

        var totalItems = await query.CountAsync(cancellationToken);

        var ratings = await query
            .Include(r => r.Movie)
            .Include(r => r.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = ratings.Select(r => new AdminRatingDto
        {
            Id = r.Id,
            MovieId = r.MovieId,
            MovieTitle = r.Movie != null ? r.Movie.Title : string.Empty,
            MovieSlug = r.Movie != null ? r.Movie.Slug : string.Empty,
            UserId = r.UserId,
            Username = r.User != null ? r.User.Username : "User",
            Rating = r.Rating,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return new PagedResult<AdminRatingDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }
}
