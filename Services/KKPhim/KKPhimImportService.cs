using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Services.KKPhim;

public class KKPhimImportService : IKKPhimImportService
{
    private readonly ApplicationDbContext _context;
    private readonly IKKPhimClient _kkphimClient;
    private readonly ILogger<KKPhimImportService> _logger;

    public KKPhimImportService(
        ApplicationDbContext context,
        IKKPhimClient kkphimClient,
        ILogger<KKPhimImportService> logger)
    {
        _context = context;
        _kkphimClient = kkphimClient;
        _logger = logger;
    }

    public async Task<MovieImportResultDto> ImportMovieBySlugAsync(
        string slug,
        bool vietsubOnly = true,
        bool englishOnly = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Movie slug cannot be empty.");
        }

        _logger.LogInformation("Starting KKPhim movie import for slug: {Slug} (VietsubOnly: {VietsubOnly}, EnglishOnly: {EnglishOnly})",
            slug, vietsubOnly, englishOnly);

        var response = await _kkphimClient.GetMovieDetailAsync(slug, cancellationToken);
        if (response?.Movie == null)
        {
            _logger.LogWarning("KKPhim returned no movie data for slug: {Slug}", slug);
            throw new NotFoundException($"Movie '{slug}' was not found on KKPhim.");
        }

        var externalMovie = response.Movie;

        // English-language validation
        if (englishOnly && !KKPhimFilterHelper.IsEnglishMovie(externalMovie))
        {
            throw new ValidationException($"Phim '{externalMovie.Name}' không thuộc danh mục phim tiếng Anh / Âu Mỹ.");
        }

        // Vietsub subtitle validation
        if (vietsubOnly && !KKPhimFilterHelper.HasVietsub(externalMovie, response.Episodes))
        {
            throw new ValidationException($"Phim '{externalMovie.Name}' không có bản phụ đề Vietsub (chỉ có Thuyết minh hoặc Lồng tiếng).");
        }

        return await ImportMovieInternalAsync(response, vietsubOnly, cancellationToken);
    }

    public async Task<KKPhimSyncResultDto> SyncCatalogAsync(
        KKPhimSyncRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var result = new KKPhimSyncResultDto();
        var fromPage = Math.Max(1, request.FromPage);
        var toPage = Math.Max(fromPage, request.ToPage);

        _logger.LogInformation(
            "Starting bulk KKPhim sync from page {FromPage} to {ToPage}, SourceType: {Source}, EnglishOnly: {EnglishOnly}, VietsubOnly: {VietsubOnly}",
            fromPage, toPage, request.SourceType, request.EnglishOnly, request.VietsubOnly);

        for (int page = fromPage; page <= toPage; page++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            _logger.LogInformation("Processing KKPhim catalog page {Page}...", page);

            KKPhimMovieListResponse? listResponse = null;
            try
            {
                if (request.SourceType.Equals("au-my", StringComparison.OrdinalIgnoreCase))
                {
                    listResponse = await _kkphimClient.GetMoviesByCountryAsync("au-my", page, cancellationToken);
                }
                else
                {
                    listResponse = await _kkphimClient.GetMovieListAsync(request.SourceType, page, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve KKPhim page {Page}", page);
                result.Errors++;
                result.ErrorMessages.Add($"Lỗi khi tải trang {page}: {ex.Message}");
                continue;
            }

            var items = listResponse?.Data?.Items;
            if (items == null || items.Count == 0)
            {
                _logger.LogWarning("No items returned on KKPhim page {Page}", page);
                continue;
            }

            result.PagesProcessed++;

            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested) break;
                result.MoviesDiscovered++;

                // 1. Quick list-level pre-filter
                if (request.EnglishOnly && !KKPhimFilterHelper.QuickPassesEnglishPreFilter(item))
                {
                    result.Skipped++;
                    continue;
                }

                if (request.VietsubOnly && !KKPhimFilterHelper.QuickPassesVietsubPreFilter(item))
                {
                    result.Skipped++;
                    continue;
                }

                // 2. Fetch full detail for validation and upsert
                KKPhimMovieDetailResponse? detailResponse = null;
                try
                {
                    detailResponse = await _kkphimClient.GetMovieDetailAsync(item.Slug, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch detail for movie {Slug}", item.Slug);
                    result.Errors++;
                    continue;
                }

                if (detailResponse?.Movie == null)
                {
                    result.Errors++;
                    continue;
                }

                var movieDetail = detailResponse.Movie;

                // 3. Strict English filtering
                if (request.EnglishOnly)
                {
                    if (!KKPhimFilterHelper.IsEnglishMovie(movieDetail))
                    {
                        result.Skipped++;
                        continue;
                    }
                    result.EnglishMovies++;
                }

                // 4. Strict Vietsub filtering
                if (request.VietsubOnly)
                {
                    if (!KKPhimFilterHelper.HasVietsub(movieDetail, detailResponse.Episodes))
                    {
                        result.Skipped++;
                        continue;
                    }
                    result.VietsubAvailable++;
                }

                // 5. Import/Upsert Movie and Vietsub episodes
                try
                {
                    var importResult = await ImportMovieInternalAsync(detailResponse, request.VietsubOnly, cancellationToken);
                    if (importResult.IsNew)
                    {
                        result.Imported++;
                        result.ImportedTitles.Add($"{importResult.Title} ({movieDetail.Year})");
                    }
                    else
                    {
                        result.Updated++;
                        result.DuplicatesPrevented++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upsert movie {Slug}", item.Slug);
                    result.Errors++;
                    result.ErrorMessages.Add($"Lỗi lưu phim '{item.Name}': {ex.Message}");
                }

                // Polite delay between requests to avoid rate limiting
                await Task.Delay(100, cancellationToken);
            }
        }

        _logger.LogInformation(
            "KKPhim sync completed. Processed: {Pages}, Discovered: {Discovered}, Imported: {Imported}, Updated: {Updated}, Skipped: {Skipped}",
            result.PagesProcessed, result.MoviesDiscovered, result.Imported, result.Updated, result.Skipped);

        return result;
    }

    private async Task<MovieImportResultDto> ImportMovieInternalAsync(
        KKPhimMovieDetailResponse response,
        bool vietsubOnly,
        CancellationToken cancellationToken)
    {
        var externalMovie = response.Movie!;
        var episodesImported = 0;
        var sourcesImported = 0;
        var genresImported = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // 1. Duplicate detection: 1st by ExternalSource + ExternalId, 2nd by Slug
            Movie? movie = null;
            if (!string.IsNullOrWhiteSpace(externalMovie.Id))
            {
                movie = await _context.Movies
                    .Include(m => m.MovieGenres)
                    .Include(m => m.Episodes)
                        .ThenInclude(e => e.Sources)
                    .FirstOrDefaultAsync(m => m.ExternalSource == "KKPHIM" && m.ExternalId == externalMovie.Id, cancellationToken);
            }

            if (movie == null)
            {
                movie = await _context.Movies
                    .Include(m => m.MovieGenres)
                    .Include(m => m.Episodes)
                        .ThenInclude(e => e.Sources)
                    .FirstOrDefaultAsync(m => m.Slug == externalMovie.Slug, cancellationToken);
            }

            var isNew = (movie == null);

            // 2. Map & Upsert Movie
            movie = KKPhimMapper.MapToMovie(externalMovie, movie);
            movie.IsVietsub = KKPhimFilterHelper.HasVietsub(externalMovie, response.Episodes);
            if (isNew)
            {
                _context.Movies.Add(movie);
                _logger.LogInformation("Inserting new movie: {Title} ({Slug})", movie.Title, movie.Slug);
            }
            else
            {
                _logger.LogInformation("Updating existing movie ID {MovieId}: {Title}", movie.Id, movie.Title);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // 3. Process Genres
            if (externalMovie.Category != null)
            {
                foreach (var categoryItem in externalMovie.Category)
                {
                    if (string.IsNullOrWhiteSpace(categoryItem.Slug) || string.IsNullOrWhiteSpace(categoryItem.Name))
                    {
                        continue;
                    }

                    var genre = await _context.Genres
                        .FirstOrDefaultAsync(g => g.Slug == categoryItem.Slug, cancellationToken);

                    if (genre == null)
                    {
                        genre = new Genre
                        {
                            Name = categoryItem.Name.Trim(),
                            Slug = categoryItem.Slug.Trim(),
                            Description = $"Genre {categoryItem.Name.Trim()}"
                        };
                        _context.Genres.Add(genre);
                        await _context.SaveChangesAsync(cancellationToken);
                        genresImported++;
                    }

                    if (!movie.MovieGenres.Any(mg => mg.GenreId == genre.Id))
                    {
                        movie.MovieGenres.Add(new MovieGenre
                        {
                            MovieId = movie.Id,
                            GenreId = genre.Id
                        });
                    }
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            // 4. Process Episodes and Playback Sources (Vietsub-only filtering)
            if (response.Episodes != null && response.Episodes.Count > 0)
            {
                foreach (var server in response.Episodes)
                {
                    var serverName = string.IsNullOrWhiteSpace(server.ServerName) ? "Default Server" : server.ServerName.Trim();

                    // If vietsubOnly is true, skip servers that are clearly dubbed (Thuyết Minh / Lồng Tiếng)
                    if (vietsubOnly && !KKPhimFilterHelper.IsVietsubServer(serverName))
                    {
                        _logger.LogInformation("Skipping dubbed server '{ServerName}' for movie '{Title}'", serverName, movie.Title);
                        continue;
                    }

                    foreach (var item in server.ServerData)
                    {
                        var episodeNumber = KKPhimMapper.ParseEpisodeNumber(item.Name);

                        // Find existing episode by (MovieId, SeasonNumber = 1, EpisodeNumber)
                        var episode = movie.Episodes.FirstOrDefault(e => e.EpisodeNumber == episodeNumber && e.SeasonNumber == 1);
                        if (episode == null)
                        {
                            episode = new Episode
                            {
                                MovieId = movie.Id,
                                EpisodeNumber = episodeNumber,
                                SeasonNumber = 1,
                                Title = item.Name.Trim(),
                                Slug = string.IsNullOrWhiteSpace(item.Slug) ? null : item.Slug.Trim(),
                                DisplayOrder = episodeNumber,
                                Description = item.Filename?.Trim(),
                                VideoUrl = item.LinkM3u8 ?? item.LinkEmbed ?? string.Empty,
                                Duration = movie.Duration,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            movie.Episodes.Add(episode);
                            await _context.SaveChangesAsync(cancellationToken);
                            episodesImported++;
                        }
                        else
                        {
                            episode.Title = item.Name.Trim();
                            if (!string.IsNullOrWhiteSpace(item.Slug))
                            {
                                episode.Slug = item.Slug.Trim();
                            }
                            if (!episode.DisplayOrder.HasValue)
                            {
                                episode.DisplayOrder = episodeNumber;
                            }
                            if (string.IsNullOrWhiteSpace(episode.VideoUrl))
                            {
                                episode.VideoUrl = item.LinkM3u8 ?? item.LinkEmbed ?? string.Empty;
                            }
                            episode.UpdatedAt = DateTime.UtcNow;
                        }

                        // Upsert EpisodeSource (HLS .m3u8 authorized stream URL - NOT downloaded locally)
                        var primaryVideoUrl = item.LinkM3u8 ?? item.LinkEmbed ?? string.Empty;
                        var existingSource = episode.Sources.FirstOrDefault(s =>
                            s.ServerName.Equals(serverName, StringComparison.OrdinalIgnoreCase) &&
                            s.SourceType == "m3u8");

                        if (existingSource == null)
                        {
                            episode.Sources.Add(new EpisodeSource
                            {
                                EpisodeId = episode.Id,
                                ServerName = serverName,
                                Language = "Vietsub",
                                SourceType = "m3u8",
                                VideoUrl = primaryVideoUrl,
                                EmbedUrl = item.LinkEmbed,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            });
                            sourcesImported++;
                        }
                        else
                        {
                            existingSource.VideoUrl = primaryVideoUrl;
                            existingSource.EmbedUrl = item.LinkEmbed;
                            existingSource.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Movie import completed for '{Title}'. IsNew: {IsNew}, Episodes: {Episodes}, Sources: {Sources}",
                movie.Title, isNew, episodesImported, sourcesImported);

            return new MovieImportResultDto
            {
                Success = true,
                Message = isNew ? "Movie imported successfully." : "Movie synchronized and updated successfully.",
                MovieId = movie.Id,
                Title = movie.Title,
                Slug = movie.Slug,
                IsNew = isNew,
                EpisodesImported = episodesImported,
                SourcesImported = sourcesImported,
                GenresImported = genresImported
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to import movie with slug: {Slug}", externalMovie.Slug);
            throw;
        }
    }

    public async Task<KKPhimMoviePreviewDto?> GetMoviePreviewAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Movie slug cannot be empty.");
        }

        var response = await _kkphimClient.GetMovieDetailAsync(slug, cancellationToken);
        if (response?.Movie == null)
        {
            return null;
        }

        var m = response.Movie;
        var serverNames = response.Episodes?.Select(e => e.ServerName).ToList() ?? new List<string>();
        var totalEpisodes = response.Episodes?.SelectMany(e => e.ServerData).Select(d => d.Slug).Distinct().Count() ?? 0;

        return new KKPhimMoviePreviewDto
        {
            ExternalId = m.Id,
            Title = m.Name,
            Slug = m.Slug,
            OriginalTitle = m.OriginName,
            Description = KKPhimMapper.StripHtml(m.Content),
            PosterUrl = KKPhimMapper.NormalizeImageUrl(m.PosterUrl),
            ThumbnailUrl = KKPhimMapper.NormalizeImageUrl(m.ThumbUrl),
            ReleaseYear = m.Year,
            Duration = KKPhimMapper.ParseDurationMinutes(m.Time),
            Country = KKPhimMapper.FormatCountries(m.Country),
            Language = m.Lang,
            MovieType = m.Type,
            Status = m.Status,
            Quality = m.Quality,
            Rating = m.Tmdb?.VoteAverage,
            Genres = m.Category?.Select(c => c.Name).ToList() ?? new List<string>(),
            TotalServerCount = serverNames.Count,
            ServerNames = serverNames,
            TotalEpisodesReported = totalEpisodes
        };
    }

    public async Task<KKPhimSearchResultDto> SearchMoviesAsync(string keyword, int page = 1, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ValidationException("Search keyword cannot be empty.");
        }

        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        var response = await _kkphimClient.SearchMoviesAsync(keyword, page, cancellationToken);
        if (response?.Data == null)
        {
            return new KKPhimSearchResultDto();
        }

        var items = response.Data.Items.Select(item => new KKPhimSearchItemDto
        {
            ExternalId = item.Id,
            Title = item.Name,
            Slug = item.Slug,
            OriginalTitle = item.OriginName,
            MovieType = item.Type,
            PosterUrl = KKPhimMapper.NormalizeImageUrl(item.PosterUrl),
            ThumbnailUrl = KKPhimMapper.NormalizeImageUrl(item.ThumbUrl),
            EpisodeCurrent = item.EpisodeCurrent,
            Quality = item.Quality,
            Language = item.Lang,
            Year = item.Year,
            Genres = item.Category?.Select(c => c.Name).ToList() ?? new List<string>(),
            Countries = item.Country?.Select(c => c.Name).ToList() ?? new List<string>()
        }).ToList();

        var pagination = response.Data.Params?.Pagination;

        return new KKPhimSearchResultDto
        {
            Items = items,
            TotalItems = pagination?.TotalItems ?? items.Count,
            CurrentPage = pagination?.CurrentPage ?? page,
            TotalPages = pagination?.TotalPages ?? (items.Count > 0 ? 1 : 0)
        };
    }
}
