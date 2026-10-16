using MovieStreaming.DTOs.Episodes;

namespace MovieStreaming.Services;

public interface IEpisodeService
{
    Task<IReadOnlyList<EpisodeListItemDto>> GetEpisodesByMovieIdAsync(int movieId, CancellationToken cancellationToken = default);
    Task<EpisodeDetailDto> GetEpisodeByIdAsync(int episodeId, CancellationToken cancellationToken = default);
    Task<EpisodeDetailDto> GetEpisodeByMovieAndIdAsync(int movieId, int episodeId, CancellationToken cancellationToken = default);
    Task<EpisodeSourcesResponseDto> GetEpisodeSourcesAsync(int episodeId, CancellationToken cancellationToken = default);
}
