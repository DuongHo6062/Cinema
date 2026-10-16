namespace MovieStreaming.Services.Authentication;

public interface ICurrentUserService
{
    int UserId { get; }
    int? TryGetUserId();
    bool IsAuthenticated { get; }
    string? Username { get; }
    string? Role { get; }
}
