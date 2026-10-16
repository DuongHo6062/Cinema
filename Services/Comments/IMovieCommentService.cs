using MovieStreaming.DTOs.Comments;

namespace MovieStreaming.Services.Comments;

public interface IMovieCommentService
{
    Task<CommentDto> AddCommentAsync(int movieId, CreateCommentRequest request, CancellationToken cancellationToken = default);
    Task<CommentListResponseDto> GetMovieCommentsAsync(int movieId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<CommentDto> EditCommentAsync(int commentId, UpdateCommentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteCommentAsync(int commentId, CancellationToken cancellationToken = default);
}
