using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Common;

namespace MovieStreaming.Services.Admin;

public class AdminEpisodeService : IAdminEpisodeService
{
    private readonly ApplicationDbContext _context;

    public AdminEpisodeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminEpisodeDto>> GetEpisodesByMovieIdAsync(int movieId, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movieExists = await _context.Movies.AnyAsync(m => m.Id == movieId, cancellationToken);
        if (!movieExists)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var episodes = await _context.Episodes
            .AsNoTracking()
            .Where(e => e.MovieId == movieId)
            .OrderBy(e => e.SeasonNumber)
            .ThenBy(e => e.DisplayOrder ?? e.EpisodeNumber)
            .ThenBy(e => e.EpisodeNumber)
            .Include(e => e.Sources)
            .ToListAsync(cancellationToken);

        return episodes.Select(MapToEpisodeDto).ToList();
    }

    public async Task<AdminEpisodeDto> GetEpisodeByIdAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var episode = await _context.Episodes
            .AsNoTracking()
            .Include(e => e.Sources)
            .FirstOrDefaultAsync(e => e.Id == episodeId, cancellationToken);

        if (episode == null)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        return MapToEpisodeDto(episode);
    }

    public async Task<AdminEpisodeDto> CreateEpisodeAsync(int movieId, AdminCreateEpisodeRequest request, CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        if (request == null || request.EpisodeNumber <= 0)
        {
            throw new ValidationException("Episode number must be greater than 0.");
        }

        var movieExists = await _context.Movies.AnyAsync(m => m.Id == movieId, cancellationToken);
        if (!movieExists)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var seasonNumber = request.SeasonNumber <= 0 ? 1 : request.SeasonNumber;

        var duplicateNumber = await _context.Episodes.AnyAsync(
            e => e.MovieId == movieId && e.SeasonNumber == seasonNumber && e.EpisodeNumber == request.EpisodeNumber,
            cancellationToken);

        if (duplicateNumber)
        {
            throw new ConflictException($"Episode {request.EpisodeNumber} already exists for season {seasonNumber} of movie {movieId}.");
        }

        var title = string.IsNullOrWhiteSpace(request.Title)
            ? $"Tập {request.EpisodeNumber:D2}"
            : request.Title.Trim();

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug($"tap-{request.EpisodeNumber}")
            : SlugHelper.GenerateSlug(request.Slug);

        var episode = new Episode
        {
            MovieId = movieId,
            EpisodeNumber = request.EpisodeNumber,
            SeasonNumber = seasonNumber,
            Title = title,
            Slug = slug,
            DisplayOrder = request.DisplayOrder ?? request.EpisodeNumber,
            Description = request.Description?.Trim(),
            VideoUrl = request.VideoUrl?.Trim(),
            Duration = request.Duration,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Episodes.AddAsync(episode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToEpisodeDto(episode);
    }

    public async Task<AdminEpisodeDto> UpdateEpisodeAsync(int episodeId, AdminUpdateEpisodeRequest request, CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        if (request == null || request.EpisodeNumber <= 0)
        {
            throw new ValidationException("Episode number must be greater than 0.");
        }

        var episode = await _context.Episodes
            .Include(e => e.Sources)
            .FirstOrDefaultAsync(e => e.Id == episodeId, cancellationToken);

        if (episode == null)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        var seasonNumber = request.SeasonNumber <= 0 ? 1 : request.SeasonNumber;

        if (request.EpisodeNumber != episode.EpisodeNumber || seasonNumber != episode.SeasonNumber)
        {
            var duplicateNumber = await _context.Episodes.AnyAsync(
                e => e.MovieId == episode.MovieId &&
                     e.SeasonNumber == seasonNumber &&
                     e.EpisodeNumber == request.EpisodeNumber &&
                     e.Id != episodeId,
                cancellationToken);

            if (duplicateNumber)
            {
                throw new ConflictException($"Episode {request.EpisodeNumber} already exists for season {seasonNumber} of movie {episode.MovieId}.");
            }
        }

        episode.EpisodeNumber = request.EpisodeNumber;
        episode.SeasonNumber = seasonNumber;
        episode.Title = string.IsNullOrWhiteSpace(request.Title) ? episode.Title : request.Title.Trim();
        episode.Slug = string.IsNullOrWhiteSpace(request.Slug) ? episode.Slug : SlugHelper.GenerateSlug(request.Slug);
        episode.DisplayOrder = request.DisplayOrder ?? episode.DisplayOrder;
        episode.Description = request.Description?.Trim();
        episode.VideoUrl = request.VideoUrl?.Trim();
        episode.Duration = request.Duration;
        episode.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToEpisodeDto(episode);
    }

    public async Task<bool> DeleteEpisodeAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var episode = await _context.Episodes
            .Include(e => e.Sources)
            .Include(e => e.WatchHistories)
            .FirstOrDefaultAsync(e => e.Id == episodeId, cancellationToken);

        if (episode == null)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        _context.EpisodeSources.RemoveRange(episode.Sources);
        _context.WatchHistories.RemoveRange(episode.WatchHistories);
        _context.Episodes.Remove(episode);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<AdminEpisodeSourceDto>> GetSourcesByEpisodeIdAsync(int episodeId, CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var episodeExists = await _context.Episodes.AnyAsync(e => e.Id == episodeId, cancellationToken);
        if (!episodeExists)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        var sources = await _context.EpisodeSources
            .AsNoTracking()
            .Where(s => s.EpisodeId == episodeId)
            .OrderBy(s => s.ServerName)
            .ToListAsync(cancellationToken);

        return sources.Select(MapToSourceDto).ToList();
    }

    public async Task<AdminEpisodeSourceDto> CreateSourceAsync(int episodeId, AdminCreateEpisodeSourceRequest request, CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.ServerName))
        {
            throw new ValidationException("Server name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.VideoUrl))
        {
            throw new ValidationException("Video URL is required.");
        }

        var episodeExists = await _context.Episodes.AnyAsync(e => e.Id == episodeId, cancellationToken);
        if (!episodeExists)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        var source = new EpisodeSource
        {
            EpisodeId = episodeId,
            ServerName = request.ServerName.Trim(),
            Language = request.Language?.Trim() ?? "Vietsub",
            SourceType = request.SourceType?.Trim() ?? "HLS",
            VideoUrl = request.VideoUrl.Trim(),
            EmbedUrl = request.EmbedUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.EpisodeSources.AddAsync(source, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToSourceDto(source);
    }

    public async Task<AdminEpisodeSourceDto> UpdateSourceAsync(int sourceId, AdminUpdateEpisodeSourceRequest request, CancellationToken cancellationToken = default)
    {
        if (sourceId <= 0)
        {
            throw new ValidationException("Source ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.ServerName))
        {
            throw new ValidationException("Server name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.VideoUrl))
        {
            throw new ValidationException("Video URL is required.");
        }

        var source = await _context.EpisodeSources
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source == null)
        {
            throw new NotFoundException($"Episode source with ID {sourceId} not found.");
        }

        source.ServerName = request.ServerName.Trim();
        source.Language = request.Language?.Trim();
        source.SourceType = request.SourceType?.Trim() ?? "HLS";
        source.VideoUrl = request.VideoUrl.Trim();
        source.EmbedUrl = request.EmbedUrl?.Trim();
        source.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToSourceDto(source);
    }

    public async Task<bool> DeleteSourceAsync(int sourceId, CancellationToken cancellationToken = default)
    {
        if (sourceId <= 0)
        {
            throw new ValidationException("Source ID must be greater than 0.");
        }

        var source = await _context.EpisodeSources
            .FirstOrDefaultAsync(s => s.Id == sourceId, cancellationToken);

        if (source == null)
        {
            throw new NotFoundException($"Episode source with ID {sourceId} not found.");
        }

        _context.EpisodeSources.Remove(source);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    private static AdminEpisodeDto MapToEpisodeDto(Episode e)
    {
        return new AdminEpisodeDto
        {
            Id = e.Id,
            MovieId = e.MovieId,
            EpisodeNumber = e.EpisodeNumber,
            SeasonNumber = e.SeasonNumber,
            Title = e.Title,
            Slug = e.Slug,
            DisplayOrder = e.DisplayOrder,
            Description = e.Description,
            VideoUrl = e.VideoUrl,
            Duration = e.Duration,
            SourceCount = e.Sources?.Count ?? 0,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
            Sources = e.Sources?.Select(MapToSourceDto).ToList() ?? new List<AdminEpisodeSourceDto>()
        };
    }

    private static AdminEpisodeSourceDto MapToSourceDto(EpisodeSource s)
    {
        return new AdminEpisodeSourceDto
        {
            Id = s.Id,
            EpisodeId = s.EpisodeId,
            ServerName = s.ServerName,
            Language = s.Language,
            SourceType = s.SourceType,
            VideoUrl = s.VideoUrl,
            EmbedUrl = s.EmbedUrl,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
}
