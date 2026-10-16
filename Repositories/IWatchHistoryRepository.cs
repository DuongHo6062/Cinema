using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IWatchHistoryRepository
{
    Task<WatchHistory?> GetByUserAndEpisodeIdAsync(int userId, int episodeId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WatchHistory> Items, int TotalCount)> GetUserWatchHistoryAsync(int userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchHistory>> GetContinueWatchingAsync(int userId, int limit = 20, CancellationToken cancellationToken = default);
    Task<WatchHistory> AddAsync(WatchHistory watchHistory, CancellationToken cancellationToken = default);
    Task UpdateAsync(WatchHistory watchHistory, CancellationToken cancellationToken = default);
    Task<bool> DeleteByUserAndEpisodeIdAsync(int userId, int episodeId, CancellationToken cancellationToken = default);
    Task<int> ClearUserHistoryAsync(int userId, CancellationToken cancellationToken = default);
}
