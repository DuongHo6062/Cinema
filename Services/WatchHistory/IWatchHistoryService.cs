using MovieStreaming.DTOs.WatchHistory;

namespace MovieStreaming.Services.WatchHistory;

public interface IWatchHistoryService
{
    Task<WatchHistoryItemDto> SaveWatchProgressAsync(SaveWatchHistoryRequest request, CancellationToken cancellationToken = default);
    Task<WatchHistoryResponseDto> GetWatchHistoryAsync(int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WatchHistoryItemDto>> GetContinueWatchingAsync(int limit = 20, CancellationToken cancellationToken = default);
    Task DeleteWatchHistoryItemAsync(int episodeId, CancellationToken cancellationToken = default);
    Task ClearWatchHistoryAsync(CancellationToken cancellationToken = default);
}
