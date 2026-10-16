using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Movies;

namespace MovieStreaming.Services;

public interface IMovieService
{
    Task<MovieListResponseDto> GetMoviesAsync(MovieQueryParameters parameters, CancellationToken cancellationToken = default);
    Task<PagedResult<MovieDto>> GetMoviesLegacyAsync(PaginationParams paginationParams);
    Task<MovieDetailDto> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<MovieDetailDto> GetMovieBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
