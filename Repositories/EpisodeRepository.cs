using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public class EpisodeRepository : IEpisodeRepository
{
    private readonly ApplicationDbContext _context;

    public EpisodeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Episode>> GetEpisodesByMovieIdAsync(int movieId, CancellationToken cancellationToken = default)
    {
        return await _context.Episodes
            .AsNoTracking()
            .Where(e => e.MovieId == movieId)
            .OrderBy(e => e.DisplayOrder.HasValue ? e.DisplayOrder.Value : e.EpisodeNumber)
            .ThenBy(e => e.EpisodeNumber)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Episode?> GetEpisodeByIdAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        return await _context.Episodes
            .AsNoTracking()
            .Include(e => e.Movie)
            .Include(e => e.Sources.OrderBy(s => s.ServerName).ThenBy(s => s.Id))
            .FirstOrDefaultAsync(e => e.Id == episodeId, cancellationToken);
    }

    public async Task<Episode?> GetEpisodeByMovieAndIdAsync(int movieId, int episodeId, CancellationToken cancellationToken = default)
    {
        return await _context.Episodes
            .AsNoTracking()
            .Include(e => e.Movie)
            .Include(e => e.Sources.OrderBy(s => s.ServerName).ThenBy(s => s.Id))
            .FirstOrDefaultAsync(e => e.MovieId == movieId && e.Id == episodeId, cancellationToken);
    }

    public async Task<IReadOnlyList<EpisodeSource>> GetEpisodeSourcesAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        return await _context.EpisodeSources
            .AsNoTracking()
            .Where(s => s.EpisodeId == episodeId && !string.IsNullOrWhiteSpace(s.VideoUrl))
            .OrderBy(s => s.ServerName)
            .ThenBy(s => s.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> MovieExistsAsync(int movieId, CancellationToken cancellationToken = default)
    {
        return await _context.Movies
            .AsNoTracking()
            .AnyAsync(m => m.Id == movieId, cancellationToken);
    }

    public async Task<bool> EpisodeExistsAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        return await _context.Episodes
            .AsNoTracking()
            .AnyAsync(e => e.Id == episodeId, cancellationToken);
    }
}
