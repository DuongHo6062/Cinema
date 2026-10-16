using System.Net;
using System.Text.Json;
using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Services.KKPhim;

public class KKPhimClient : IKKPhimClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KKPhimClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KKPhimClient(HttpClient httpClient, ILogger<KKPhimClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<KKPhimMovieDetailResponse?> GetMovieDetailAsync(string slug, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/phim/{Uri.EscapeDataString(slug)}";
        return await SendGetRequestAsync<KKPhimMovieDetailResponse>(endpoint, cancellationToken);
    }

    public async Task<KKPhimSearchResponse?> SearchMoviesAsync(string keyword, int page = 1, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/v1/api/tim-kiem?keyword={Uri.EscapeDataString(keyword)}&page={page}";
        return await SendGetRequestAsync<KKPhimSearchResponse>(endpoint, cancellationToken);
    }

    public async Task<KKPhimMovieListResponse?> GetHomeAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/v1/api/home";
        return await SendGetRequestAsync<KKPhimMovieListResponse>(endpoint, cancellationToken);
    }

    public async Task<KKPhimMovieListResponse?> GetMovieListAsync(string listType = "phim-moi-cap-nhat", int page = 1, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/v1/api/danh-sach/{Uri.EscapeDataString(listType)}?page={page}";
        return await SendGetRequestAsync<KKPhimMovieListResponse>(endpoint, cancellationToken);
    }

    public async Task<KKPhimMovieListResponse?> GetMoviesByCountryAsync(string countrySlug = "au-my", int page = 1, CancellationToken cancellationToken = default)
    {
        var endpoint = $"/v1/api/quoc-gia/{Uri.EscapeDataString(countrySlug)}?page={page}";
        return await SendGetRequestAsync<KKPhimMovieListResponse>(endpoint, cancellationToken);
    }

    public async Task<KKPhimGenreListResponse?> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/v1/api/the-loai";
        return await SendGetRequestAsync<KKPhimGenreListResponse>(endpoint, cancellationToken);
    }

    public async Task<KKPhimCountryListResponse?> GetCountriesAsync(CancellationToken cancellationToken = default)
    {
        const string endpoint = "/v1/api/quoc-gia";
        return await SendGetRequestAsync<KKPhimCountryListResponse>(endpoint, cancellationToken);
    }

    private async Task<T?> SendGetRequestAsync<T>(string endpoint, CancellationToken cancellationToken) where T : class
    {
        try
        {
            _logger.LogInformation("Sending KKPhim API request to: {Endpoint}", endpoint);

            using var response = await _httpClient.GetAsync(endpoint, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("KKPhim endpoint {Endpoint} returned 404 Not Found.", endpoint);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("KKPhim API request to {Endpoint} failed with HTTP {StatusCode}", endpoint, (int)response.StatusCode);
                return null;
            }

            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var result = await JsonSerializer.DeserializeAsync<T>(contentStream, _jsonOptions, cancellationToken);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("KKPhim request to {Endpoint} was cancelled.", endpoint);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error calling KKPhim API endpoint: {Endpoint}", endpoint);
            return null;
        }
    }
}
