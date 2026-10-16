using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Services.KKPhim;

public interface IKKPhimClient
{
    Task<KKPhimMovieDetailResponse?> GetMovieDetailAsync(string slug, CancellationToken cancellationToken = default);
    Task<KKPhimSearchResponse?> SearchMoviesAsync(string keyword, int page = 1, CancellationToken cancellationToken = default);
    Task<KKPhimMovieListResponse?> GetHomeAsync(CancellationToken cancellationToken = default);
    Task<KKPhimMovieListResponse?> GetMovieListAsync(string listType = "phim-moi-cap-nhat", int page = 1, CancellationToken cancellationToken = default);
    Task<KKPhimMovieListResponse?> GetMoviesByCountryAsync(string countrySlug = "au-my", int page = 1, CancellationToken cancellationToken = default);
    Task<KKPhimGenreListResponse?> GetGenresAsync(CancellationToken cancellationToken = default);
    Task<KKPhimCountryListResponse?> GetCountriesAsync(CancellationToken cancellationToken = default);
}
