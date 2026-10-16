using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IMovieCommentRepository
{
    Task<MovieComment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<MovieComment> Items, int TotalCount)> GetByMovieIdPagedAsync(int movieId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MovieComment> AddAsync(MovieComment comment, CancellationToken cancellationToken = default);
    Task UpdateAsync(MovieComment comment, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(MovieComment comment, CancellationToken cancellationToken = default);
}
