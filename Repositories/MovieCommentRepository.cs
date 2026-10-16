using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public class MovieCommentRepository : IMovieCommentRepository
{
    private readonly ApplicationDbContext _context;

    public MovieCommentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MovieComment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.MovieComments
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<MovieComment> Items, int TotalCount)> GetByMovieIdPagedAsync(
        int movieId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MovieComments
            .AsNoTracking()
            .Where(c => c.MovieId == movieId)
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(c => c.User)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<MovieComment> AddAsync(MovieComment comment, CancellationToken cancellationToken = default)
    {
        await _context.MovieComments.AddAsync(comment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task UpdateAsync(MovieComment comment, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(comment);
        if (entry.State == EntityState.Detached)
        {
            _context.MovieComments.Update(comment);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(MovieComment comment, CancellationToken cancellationToken = default)
    {
        _context.MovieComments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
