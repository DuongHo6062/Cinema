using MovieStreaming.Models.Entities;

namespace MovieStreaming.Services.Authentication;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    RefreshToken GenerateRefreshToken(int userId);
    int GetAccessTokenExpirationSeconds();
}
