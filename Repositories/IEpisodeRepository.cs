using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IEpisodeRepository
{
    Task<IReadOnlyList<Episode>> GetEpisodesByMovieIdAsync(int movieId, CancellationToken cancellationToken = default);
    Task<Episode?> GetEpisodeByIdAsync(int episodeId, CancellationToken cancellationToken = default);
    Task<Episode?> GetEpisodeByMovieAndIdAsync(int movieId, int episodeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EpisodeSource>> GetEpisodeSourcesAsync(int episodeId, CancellationToken cancellationToken = default);
    Task<bool> MovieExistsAsync(int movieId, CancellationToken cancellationToken = default);
    Task<bool> EpisodeExistsAsync(int episodeId, CancellationToken cancellationToken = default);
}
