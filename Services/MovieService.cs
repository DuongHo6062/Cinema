using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Movies;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;

namespace MovieStreaming.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;

    public MovieService(IMovieRepository movieRepository)
    {
        _movieRepository = movieRepository;
    }

    public async Task<MovieListResponseDto> GetMoviesAsync(
        MovieQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        if (parameters.Page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (parameters.PageSize < 1 || parameters.PageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var (items, totalCount) = await _movieRepository.GetMoviesAsync(parameters, cancellationToken);

        var dtos = items.Select(MapToMovieListItemDto).ToList();

        return new MovieListResponseDto
        {
            Items = dtos,
            TotalItems = totalCount,
            Page = parameters.Page,
            PageSize = parameters.PageSize
        };
    }

    public async Task<PagedResult<MovieDto>> GetMoviesLegacyAsync(PaginationParams paginationParams)
    {
        var parameters = new MovieQueryParameters
        {
            Page = paginationParams.Page,
            PageSize = paginationParams.PageSize
        };

        var response = await GetMoviesAsync(parameters);

        return new PagedResult<MovieDto>
        {
            Items = response.Items.Select(item => new MovieDto
            {
                Id = item.Id,
                Title = item.Title,
                Slug = item.Slug,
                OriginalTitle = item.OriginalTitle,
                Description = item.Description,
                PosterUrl = item.PosterUrl,
                ThumbnailUrl = item.ThumbnailUrl,
                ReleaseYear = item.ReleaseYear,
                Duration = item.Duration,
                Country = item.Country,
                Language = item.Language,
                MovieType = item.MovieType,
                Status = item.Status,
                Quality = item.Quality,
                Rating = item.Rating,
                CreatedAt = item.CreatedAt,
                Genres = item.Genres
            }).ToList(),
            TotalItems = response.TotalItems,
            Page = response.Page,
            PageSize = response.PageSize
        };
    }

    public async Task<MovieDetailDto> GetMovieByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(id, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {id} not found.");
        }

        return MapToMovieDetailDto(movie);
    }

    public async Task<MovieDetailDto> GetMovieBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Movie slug cannot be empty.");
        }

        var movie = await _movieRepository.GetMovieBySlugAsync(slug.Trim(), cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with slug '{slug.Trim()}' not found.");
        }

        return MapToMovieDetailDto(movie);
    }

    private static MovieListItemDto MapToMovieListItemDto(Movie movie)
    {
        return new MovieListItemDto
        {
            Id = movie.Id,
            Title = movie.Title,
            Slug = movie.Slug,
            OriginalTitle = movie.OriginalTitle,
            Description = movie.Description,
            PosterUrl = movie.PosterUrl,
            ThumbnailUrl = movie.ThumbnailUrl,
            ReleaseYear = movie.ReleaseYear,
            Duration = movie.Duration,
            Country = movie.Country,
            Language = movie.Language,
            MovieType = movie.MovieType,
            Status = movie.Status,
            Quality = movie.Quality,
            Rating = movie.Rating,
            CreatedAt = movie.CreatedAt,
            Genres = movie.MovieGenres.Select(mg => new GenreDto
            {
                Id = mg.Genre.Id,
                Name = mg.Genre.Name,
                Slug = mg.Genre.Slug,
                Description = mg.Genre.Description
            }).ToList()
        };
    }

    private static MovieDetailDto MapToMovieDetailDto(Movie movie)
    {
        return new MovieDetailDto
        {
            Id = movie.Id,
            Title = movie.Title,
            Slug = movie.Slug,
            OriginalTitle = movie.OriginalTitle,
            Description = movie.Description,
            PosterUrl = movie.PosterUrl,
            ThumbnailUrl = movie.ThumbnailUrl,
            ReleaseYear = movie.ReleaseYear,
            Duration = movie.Duration,
            Country = movie.Country,
            Language = movie.Language,
            MovieType = movie.MovieType,
            Status = movie.Status,
            Quality = movie.Quality,
            Rating = movie.Rating,
            CreatedAt = movie.CreatedAt,
            UpdatedAt = movie.UpdatedAt,
            Genres = movie.MovieGenres.Select(mg => new GenreDto
            {
                Id = mg.Genre.Id,
                Name = mg.Genre.Name,
                Slug = mg.Genre.Slug,
                Description = mg.Genre.Description
            }).ToList(),
            Episodes = movie.Episodes
                .OrderBy(e => e.DisplayOrder.HasValue ? e.DisplayOrder.Value : e.EpisodeNumber)
                .ThenBy(e => e.EpisodeNumber)
                .ThenBy(e => e.Id)
                .Select(e => new EpisodeDto
                {
                    Id = e.Id,
                    MovieId = e.MovieId,
                    EpisodeNumber = e.EpisodeNumber,
                    EpisodeName = e.Title,
                    SeasonNumber = e.SeasonNumber,
                    Title = e.Title,
                    DisplayOrder = e.DisplayOrder ?? e.EpisodeNumber,
                    Slug = e.Slug,
                    Description = e.Description,
                    VideoUrl = e.VideoUrl,
                    Duration = e.Duration,
                    Sources = e.Sources
                        .Where(s => !string.IsNullOrWhiteSpace(s.VideoUrl))
                        .OrderBy(s => s.ServerName)
                        .ThenBy(s => s.Id)
                        .Select(s => new EpisodeSourceDto
                        {
                            Id = s.Id,
                            ServerName = s.ServerName,
                            Language = s.Language,
                            SourceType = SourceTypeResolver.Resolve(s.SourceType, s.VideoUrl),
                            VideoUrl = s.VideoUrl,
                            EmbedUrl = s.EmbedUrl
                        }).ToList()
                }).ToList()
        };
    }
}
