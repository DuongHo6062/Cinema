using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminEpisodeService
{
    Task<IReadOnlyList<AdminEpisodeDto>> GetEpisodesByMovieIdAsync(int movieId, CancellationToken cancellationToken = default);
    Task<AdminEpisodeDto> GetEpisodeByIdAsync(int episodeId, CancellationToken cancellationToken = default);
    Task<AdminEpisodeDto> CreateEpisodeAsync(int movieId, AdminCreateEpisodeRequest request, CancellationToken cancellationToken = default);
    Task<AdminEpisodeDto> UpdateEpisodeAsync(int episodeId, AdminUpdateEpisodeRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEpisodeAsync(int episodeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminEpisodeSourceDto>> GetSourcesByEpisodeIdAsync(int episodeId, CancellationToken cancellationToken = default);
    Task<AdminEpisodeSourceDto> CreateSourceAsync(int episodeId, AdminCreateEpisodeSourceRequest request, CancellationToken cancellationToken = default);
    Task<AdminEpisodeSourceDto> UpdateSourceAsync(int sourceId, AdminUpdateEpisodeSourceRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteSourceAsync(int sourceId, CancellationToken cancellationToken = default);
}
