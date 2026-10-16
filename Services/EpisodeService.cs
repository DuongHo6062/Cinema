using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Episodes;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;

namespace MovieStreaming.Services;

public class EpisodeService : IEpisodeService
{
    private readonly IEpisodeRepository _episodeRepository;

    public EpisodeService(IEpisodeRepository episodeRepository)
    {
        _episodeRepository = episodeRepository;
    }

    public async Task<IReadOnlyList<EpisodeListItemDto>> GetEpisodesByMovieIdAsync(
        int movieId,
        CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movieExists = await _episodeRepository.MovieExistsAsync(movieId, cancellationToken);
        if (!movieExists)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var episodes = await _episodeRepository.GetEpisodesByMovieIdAsync(movieId, cancellationToken);

        return episodes.Select(e => new EpisodeListItemDto
        {
            Id = e.Id,
            MovieId = e.MovieId,
            EpisodeNumber = e.EpisodeNumber,
            EpisodeName = e.Title,
            Title = e.Title,
            SeasonNumber = e.SeasonNumber,
            DisplayOrder = e.DisplayOrder ?? e.EpisodeNumber,
            Slug = e.Slug,
            Duration = e.Duration,
            Description = e.Description
        }).ToList();
    }

    public async Task<EpisodeDetailDto> GetEpisodeByIdAsync(
        int episodeId,
        CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var episode = await _episodeRepository.GetEpisodeByIdAsync(episodeId, cancellationToken);
        if (episode == null)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        return MapToEpisodeDetailDto(episode);
    }

    public async Task<EpisodeDetailDto> GetEpisodeByMovieAndIdAsync(
        int movieId,
        int episodeId,
        CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var movieExists = await _episodeRepository.MovieExistsAsync(movieId, cancellationToken);
        if (!movieExists)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var episode = await _episodeRepository.GetEpisodeByIdAsync(episodeId, cancellationToken);
        if (episode == null)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        if (episode.MovieId != movieId)
        {
            throw new NotFoundException($"Episode with ID {episodeId} does not belong to movie with ID {movieId}.");
        }

        return MapToEpisodeDetailDto(episode);
    }

    public async Task<EpisodeSourcesResponseDto> GetEpisodeSourcesAsync(
        int episodeId,
        CancellationToken cancellationToken = default)
    {
        if (episodeId <= 0)
        {
            throw new ValidationException("Episode ID must be greater than 0.");
        }

        var episodeExists = await _episodeRepository.EpisodeExistsAsync(episodeId, cancellationToken);
        if (!episodeExists)
        {
            throw new NotFoundException($"Episode with ID {episodeId} not found.");
        }

        var sources = await _episodeRepository.GetEpisodeSourcesAsync(episodeId, cancellationToken);

        var validSources = sources
            .Where(s => !string.IsNullOrWhiteSpace(s.VideoUrl))
            .OrderByDescending(s => s.ServerName != null && s.ServerName.ToLower().Contains("vietsub"))
            .ThenBy(s => s.ServerName != null && (s.ServerName.ToLower().Contains("thuyết minh") || s.ServerName.ToLower().Contains("thuyet minh") || s.ServerName.ToLower().Contains("lồng tiếng") || s.ServerName.ToLower().Contains("long tieng")))
            .ThenBy(s => s.ServerName)
            .ThenBy(s => s.Id)
            .Select(s => new EpisodeSourceDto
            {
                Id = s.Id,
                ServerName = s.ServerName,
                Language = s.Language,
                SourceType = SourceTypeResolver.Resolve(s.SourceType, s.VideoUrl),
                VideoUrl = s.VideoUrl,
                EmbedUrl = s.EmbedUrl
            }).ToList();

        return new EpisodeSourcesResponseDto
        {
            EpisodeId = episodeId,
            Sources = validSources
        };
    }

    private static EpisodeDetailDto MapToEpisodeDetailDto(Episode episode)
    {
        var validSources = episode.Sources
            .Where(s => !string.IsNullOrWhiteSpace(s.VideoUrl))
            .OrderByDescending(s => s.ServerName != null && s.ServerName.ToLower().Contains("vietsub"))
            .ThenBy(s => s.ServerName != null && (s.ServerName.ToLower().Contains("thuyết minh") || s.ServerName.ToLower().Contains("thuyet minh") || s.ServerName.ToLower().Contains("lồng tiếng") || s.ServerName.ToLower().Contains("long tieng")))
            .ThenBy(s => s.ServerName)
            .ThenBy(s => s.Id)
            .Select(s => new EpisodeSourceDto
            {
                Id = s.Id,
                ServerName = s.ServerName,
                Language = s.Language,
                SourceType = SourceTypeResolver.Resolve(s.SourceType, s.VideoUrl),
                VideoUrl = s.VideoUrl,
                EmbedUrl = s.EmbedUrl
            }).ToList();

        return new EpisodeDetailDto
        {
            Id = episode.Id,
            MovieId = episode.MovieId,
            MovieTitle = episode.Movie?.Title,
            EpisodeNumber = episode.EpisodeNumber,
            EpisodeName = episode.Title,
            Title = episode.Title,
            SeasonNumber = episode.SeasonNumber,
            DisplayOrder = episode.DisplayOrder ?? episode.EpisodeNumber,
            Slug = episode.Slug,
            Description = episode.Description,
            Duration = episode.Duration,
            Sources = validSources
        };
    }
}
