using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminGenreService
{
    Task<IReadOnlyList<AdminGenreDto>> GetAllGenresAsync(CancellationToken cancellationToken = default);
    Task<AdminGenreDto> GetGenreByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminGenreDto> CreateGenreAsync(AdminCreateGenreRequest request, CancellationToken cancellationToken = default);
    Task<AdminGenreDto> UpdateGenreAsync(int id, AdminUpdateGenreRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteGenreAsync(int id, CancellationToken cancellationToken = default);
}
