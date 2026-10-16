using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminMovieService
{
    Task<PagedResult<AdminMovieListItemDto>> GetMoviesAsync(
        string? search,
        int? genreId,
        int? year,
        string? status,
        string? sortBy,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<AdminMovieDetailDto> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminMovieDetailDto> CreateMovieAsync(AdminCreateMovieRequest request, CancellationToken cancellationToken = default);
    Task<AdminMovieDetailDto> UpdateMovieAsync(int id, AdminUpdateMovieRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteMovieAsync(int id, CancellationToken cancellationToken = default);
}
