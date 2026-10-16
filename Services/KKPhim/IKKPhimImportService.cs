using MovieStreaming.DTOs;

namespace MovieStreaming.Services.KKPhim;

public interface IKKPhimImportService
{
    Task<MovieImportResultDto> ImportMovieBySlugAsync(string slug, bool vietsubOnly = true, bool englishOnly = false, CancellationToken cancellationToken = default);
    Task<KKPhimSyncResultDto> SyncCatalogAsync(KKPhimSyncRequestDto request, CancellationToken cancellationToken = default);
    Task<KKPhimMoviePreviewDto?> GetMoviePreviewAsync(string slug, CancellationToken cancellationToken = default);
    Task<KKPhimSearchResultDto> SearchMoviesAsync(string keyword, int page = 1, CancellationToken cancellationToken = default);
}
