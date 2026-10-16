using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminModerationService
{
    Task<PagedResult<AdminCommentDto>> GetCommentsAsync(
        int? movieId,
        int? userId,
        string? search,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<AdminCommentDto> GetCommentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminRatingDto>> GetRatingsAsync(
        int? movieId,
        int? userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
