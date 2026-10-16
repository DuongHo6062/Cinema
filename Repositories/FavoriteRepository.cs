using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly ApplicationDbContext _context;

    public FavoriteRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Favorite?> GetByUserAndMovieIdAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .Include(f => f.Movie)
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId, cancellationToken);
    }

    public async Task<bool> IsFavoriteAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Favorites
            .AsNoTracking()
            .AnyAsync(f => f.UserId == userId && f.MovieId == movieId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Favorite> Items, int TotalCount)> GetUserFavoritesAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(f => f.Movie)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Favorite> AddAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        await _context.Favorites.AddAsync(favorite, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return favorite;
    }

    public async Task<bool> DeleteByUserAndMovieIdAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.MovieId == movieId, cancellationToken);

        if (record == null)
        {
            return false;
        }

        _context.Favorites.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
