using MovieStreaming.DTOs.Comments;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Services.Comments;

public class MovieCommentService : IMovieCommentService
{
    private readonly IMovieCommentRepository _movieCommentRepository;
    private readonly IMovieRepository _movieRepository;
    private readonly ICurrentUserService _currentUserService;

    public MovieCommentService(
        IMovieCommentRepository movieCommentRepository,
        IMovieRepository movieRepository,
        ICurrentUserService currentUserService)
    {
        _movieCommentRepository = movieCommentRepository;
        _movieRepository = movieRepository;
        _currentUserService = currentUserService;
    }

    public async Task<CommentDto> AddCommentAsync(
        int movieId,
        CreateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ValidationException("Comment content cannot be empty.");
        }

        var trimmedContent = request.Content.Trim();
        if (trimmedContent.Length > 1000)
        {
            throw new ValidationException("Comment content cannot exceed 1000 characters.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var userId = _currentUserService.UserId;
        var username = _currentUserService.Username ?? "User";

        var comment = new MovieComment
        {
            UserId = userId,
            MovieId = movieId,
            Content = trimmedContent,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _movieCommentRepository.AddAsync(comment, cancellationToken);

        return new CommentDto
        {
            Id = created.Id,
            MovieId = created.MovieId,
            UserId = created.UserId,
            Username = username,
            Content = created.Content,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt,
            IsOwner = true
        };
    }

    public async Task<CommentListResponseDto> GetMovieCommentsAsync(
        int movieId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (movieId <= 0)
        {
            throw new ValidationException("Movie ID must be greater than 0.");
        }

        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var movie = await _movieRepository.GetMovieByIdAsync(movieId, cancellationToken);
        if (movie == null)
        {
            throw new NotFoundException($"Movie with ID {movieId} not found.");
        }

        var currentUserId = _currentUserService.TryGetUserId();
        var (items, totalCount) = await _movieCommentRepository.GetByMovieIdPagedAsync(movieId, page, pageSize, cancellationToken);

        var commentDtos = items.Select(c => new CommentDto
        {
            Id = c.Id,
            MovieId = c.MovieId,
            UserId = c.UserId,
            Username = c.User?.Username ?? "Anonymous",
            Content = c.Content,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            IsOwner = currentUserId.HasValue && c.UserId == currentUserId.Value
        }).ToList();

        return new CommentListResponseDto
        {
            Items = commentDtos,
            TotalItems = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CommentDto> EditCommentAsync(
        int commentId,
        UpdateCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (commentId <= 0)
        {
            throw new ValidationException("Comment ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Content))
        {
            throw new ValidationException("Comment content cannot be empty.");
        }

        var trimmedContent = request.Content.Trim();
        if (trimmedContent.Length > 1000)
        {
            throw new ValidationException("Comment content cannot exceed 1000 characters.");
        }

        var comment = await _movieCommentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
        {
            throw new NotFoundException($"Comment with ID {commentId} not found.");
        }

        var userId = _currentUserService.UserId;
        if (comment.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to edit this comment.");
        }

        comment.Content = trimmedContent;
        comment.UpdatedAt = DateTime.UtcNow;

        await _movieCommentRepository.UpdateAsync(comment, cancellationToken);

        return new CommentDto
        {
            Id = comment.Id,
            MovieId = comment.MovieId,
            UserId = comment.UserId,
            Username = comment.User?.Username ?? _currentUserService.Username ?? "User",
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            IsOwner = true
        };
    }

    public async Task<bool> DeleteCommentAsync(int commentId, CancellationToken cancellationToken = default)
    {
        if (commentId <= 0)
        {
            throw new ValidationException("Comment ID must be greater than 0.");
        }

        var comment = await _movieCommentRepository.GetByIdAsync(commentId, cancellationToken);
        if (comment == null)
        {
            throw new NotFoundException($"Comment with ID {commentId} not found.");
        }

        var userId = _currentUserService.UserId;
        if (comment.UserId != userId)
        {
            throw new ForbiddenException("You do not have permission to delete this comment.");
        }

        await _movieCommentRepository.DeleteAsync(comment, cancellationToken);
        return true;
    }
}
