using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.WatchHistory;

namespace MovieStreaming.Repositories;

public class WatchHistoryRepository : IWatchHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public WatchHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<WatchHistory?> GetByUserAndEpisodeIdAsync(
        int userId,
        int episodeId,
        CancellationToken cancellationToken = default)
    {
        return await _context.WatchHistories
            .Include(wh => wh.Movie)
            .Include(wh => wh.Episode)
            .FirstOrDefaultAsync(wh => wh.UserId == userId && wh.EpisodeId == episodeId, cancellationToken);
    }

    public async Task<(IReadOnlyList<WatchHistory> Items, int TotalCount)> GetUserWatchHistoryAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.WatchHistories
            .AsNoTracking()
            .Where(wh => wh.UserId == userId)
            .OrderByDescending(wh => wh.LastWatchedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Include(wh => wh.Movie)
            .Include(wh => wh.Episode)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<WatchHistory>> GetContinueWatchingAsync(
        int userId,
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        return await _context.WatchHistories
            .AsNoTracking()
            .Where(wh => wh.UserId == userId &&
                         wh.ProgressSeconds > 0 &&
                         (wh.DurationSeconds == null || wh.DurationSeconds <= 0 ||
                          (double)wh.ProgressSeconds / wh.DurationSeconds.Value < 0.90))
            .OrderByDescending(wh => wh.LastWatchedAt)
            .Include(wh => wh.Movie)
            .Include(wh => wh.Episode)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<WatchHistory> AddAsync(WatchHistory watchHistory, CancellationToken cancellationToken = default)
    {
        await _context.WatchHistories.AddAsync(watchHistory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return watchHistory;
    }

    public async Task UpdateAsync(WatchHistory watchHistory, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(watchHistory);
        if (entry.State == EntityState.Detached)
        {
            _context.WatchHistories.Update(watchHistory);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteByUserAndEpisodeIdAsync(
        int userId,
        int episodeId,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.WatchHistories
            .FirstOrDefaultAsync(wh => wh.UserId == userId && wh.EpisodeId == episodeId, cancellationToken);

        if (record == null)
        {
            return false;
        }

        _context.WatchHistories.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> ClearUserHistoryAsync(int userId, CancellationToken cancellationToken = default)
    {
        var records = await _context.WatchHistories
            .Where(wh => wh.UserId == userId)
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
        {
            return 0;
        }

        _context.WatchHistories.RemoveRange(records);
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
