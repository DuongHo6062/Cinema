using MovieStreaming.DTOs.Movies;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IMovieRepository
{
    Task<(IEnumerable<Movie> Items, int TotalCount)> GetMoviesAsync(MovieQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Movie> Items, int TotalCount)> GetPagedMoviesAsync(int page, int pageSize);
    Task<Movie?> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Movie?> GetMovieBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
