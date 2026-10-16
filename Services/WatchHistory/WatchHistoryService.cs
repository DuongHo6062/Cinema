using MovieStreaming.DTOs.WatchHistory;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services.Authentication;
using WatchHistoryEntity = MovieStreaming.Models.Entities.WatchHistory;

namespace MovieStreaming.Services.WatchHistory;

public class WatchHistoryService : IWatchHistoryService
{
    private readonly IWatchHistoryRepository _watchHistoryRepository;
    private readonly IEpisodeRepository _episodeRepository;
    private readonly ICurrentUserService _currentUserService;

    public WatchHistoryService(
        IWatchHistoryRepository watchHistoryRepository,
        IEpisodeRepository episodeRepository,
        ICurrentUserService currentUserService)
    {
        _watchHistoryRepository = watchHistoryRepository;
        _episodeRepository = episodeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<WatchHistoryItemDto> SaveWatchProgressAsync(
        SaveWatchHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (request.EpisodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        if (request.ProgressSeconds < 0)
        {
            throw new ValidationException("Progress seconds cannot be negative.");
        }

        if (request.DurationSeconds.HasValue && request.DurationSeconds.Value < 0)
        {
            throw new ValidationException("Duration seconds cannot be negative.");
        }

        var episode = await _episodeRepository.GetEpisodeByIdAsync(request.EpisodeId, cancellationToken);
        if (episode == null)
        {
            throw new NotFoundException($"Episode with ID {request.EpisodeId} not found.");
        }

        var progress = request.ProgressSeconds;
        var duration = request.DurationSeconds ?? (episode.Duration.HasValue ? episode.Duration.Value * 60 : (int?)null);

        // Handle small client-side inaccuracies safely
        if (duration.HasValue && duration.Value > 0)
        {
            if (progress > duration.Value * 2)
            {
                throw new ValidationException("Progress seconds cannot unreasonably exceed video duration.");
            }

            if (progress > duration.Value)
            {
                progress = duration.Value;
            }
        }

        var existingRecord = await _watchHistoryRepository.GetByUserAndEpisodeIdAsync(userId, request.EpisodeId, cancellationToken);
        WatchHistoryEntity record;

        if (existingRecord != null)
        {
            existingRecord.ProgressSeconds = progress;
            if (duration.HasValue)
            {
                existingRecord.DurationSeconds = duration.Value;
            }
            existingRecord.LastWatchedAt = DateTime.UtcNow;
            existingRecord.UpdatedAt = DateTime.UtcNow;

            await _watchHistoryRepository.UpdateAsync(existingRecord, cancellationToken);
            record = existingRecord;
        }
        else
        {
            record = new WatchHistoryEntity
            {
                UserId = userId,
                MovieId = episode.MovieId,
                EpisodeId = episode.Id,
                ProgressSeconds = progress,
                DurationSeconds = duration,
                LastWatchedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _watchHistoryRepository.AddAsync(record, cancellationToken);
        }

        return MapToItemDto(record, episode);
    }

    public async Task<WatchHistoryResponseDto> GetWatchHistoryAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var userId = _currentUserService.UserId;
        var (items, totalCount) = await _watchHistoryRepository.GetUserWatchHistoryAsync(userId, page, pageSize, cancellationToken);

        return new WatchHistoryResponseDto
        {
            Items = items.Select(wh => MapToItemDto(wh)).ToList(),
            TotalItems = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<WatchHistoryItemDto>> GetContinueWatchingAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (limit < 1 || limit > 100)
        {
            limit = 20;
        }

        var userId = _currentUserService.UserId;
        var records = await _watchHistoryRepository.GetContinueWatchingAsync(userId, limit, cancellationToken);

        return records
            .Where(r => !WatchProgressRules.IsCompleted(r.ProgressSeconds, r.DurationSeconds))
            .Select(wh => MapToItemDto(wh))
            .ToList();
    }

    public async Task DeleteWatchHistoryItemAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var userId = _currentUserService.UserId;
        var deleted = await _watchHistoryRepository.DeleteByUserAndEpisodeIdAsync(userId, episodeId, cancellationToken);

        if (!deleted)
        {
            throw new NotFoundException($"Watch history for episode ID {episodeId} not found.");
        }
    }

    public async Task ClearWatchHistoryAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        await _watchHistoryRepository.ClearUserHistoryAsync(userId, cancellationToken);
    }

    private static WatchHistoryItemDto MapToItemDto(WatchHistoryEntity wh, Episode? fallbackEpisode = null)
    {
        var duration = wh.DurationSeconds;
        var progress = wh.ProgressSeconds;
        var completed = WatchProgressRules.IsCompleted(progress, duration);
        var percentage = WatchProgressRules.CalculateProgressPercentage(progress, duration);

        var episode = wh.Episode ?? fallbackEpisode;
        var movie = wh.Movie ?? fallbackEpisode?.Movie;

        return new WatchHistoryItemDto
        {
            Id = wh.Id,
            MovieId = wh.MovieId,
            MovieTitle = movie?.Title ?? string.Empty,
            MovieSlug = movie?.Slug ?? string.Empty,
            PosterUrl = movie?.PosterUrl,
            EpisodeId = wh.EpisodeId,
            EpisodeNumber = episode?.EpisodeNumber ?? 0,
            EpisodeName = episode?.Title ?? string.Empty,
            SeasonNumber = episode?.SeasonNumber ?? 1,
            ProgressSeconds = progress,
            DurationSeconds = duration,
            ProgressPercentage = percentage,
            Completed = completed,
            LastWatchedAt = wh.LastWatchedAt
        };
    }
}
