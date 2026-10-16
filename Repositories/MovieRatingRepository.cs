using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public class MovieRatingRepository : IMovieRatingRepository
{
    private readonly ApplicationDbContext _context;

    public MovieRatingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<MovieRating?> GetByUserAndMovieIdAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken = default)
    {
        return await _context.MovieRatings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId, cancellationToken);
    }

    public async Task<MovieRating> AddAsync(MovieRating rating, CancellationToken cancellationToken = default)
    {
        await _context.MovieRatings.AddAsync(rating, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return rating;
    }

    public async Task UpdateAsync(MovieRating rating, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(rating);
        if (entry.State == EntityState.Detached)
        {
            _context.MovieRatings.Update(rating);
        }
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteByUserAndMovieIdAsync(
        int userId,
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var record = await _context.MovieRatings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.MovieId == movieId, cancellationToken);

        if (record == null)
        {
            return false;
        }

        _context.MovieRatings.Remove(record);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<(decimal AverageRating, int TotalRatings, Dictionary<int, int> Distribution)> GetRatingSummaryAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MovieRatings
            .AsNoTracking()
            .Where(r => r.MovieId == movieId);

        var totalRatings = await query.CountAsync(cancellationToken);
        if (totalRatings == 0)
        {
            var emptyDistribution = new Dictionary<int, int>();
            for (int i = 1; i <= 10; i++)
            {
                emptyDistribution[i] = 0;
            }
            return (0.0m, 0, emptyDistribution);
        }

        var average = await query.AverageAsync(r => (double)r.Rating, cancellationToken);
        var roundedAverage = Math.Round((decimal)average, 1, MidpointRounding.AwayFromZero);

        var distributionData = await query
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var distribution = new Dictionary<int, int>();
        for (int i = 1; i <= 10; i++)
        {
            distribution[i] = 0;
        }
        foreach (var item in distributionData)
        {
            distribution[item.Rating] = item.Count;
        }

        return (roundedAverage, totalRatings, distribution);
    }
}
