using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MovieStreaming.Configurations;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Tests;

public class TokenServiceTests
{
    private readonly JwtOptions _jwtOptions;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _jwtOptions = new JwtOptions
        {
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            Key = "TestSecretKeyThatIsAtLeast32BytesLongForHmacSha256Security12345!",
            AccessTokenExpirationMinutes = 30,
            RefreshTokenExpirationDays = 5
        };

        var optionsMock = Options.Create(_jwtOptions);
        _tokenService = new TokenService(optionsMock, NullLogger<TokenService>.Instance);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ReturnsValidJwtWithCorrectClaims()
    {
        // Arrange
        var user = new User
        {
            Id = 42,
            Username = "johndoe",
            Email = "john@example.com",
            Role = new Role { Id = 1, Name = "User" }
        };

        // Act
        var tokenString = _tokenService.GenerateAccessToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(tokenString));

        var jwtToken = handler.ReadJwtToken(tokenString);
        Assert.Equal(_jwtOptions.Issuer, jwtToken.Issuer);
        Assert.Contains(_jwtOptions.Audience, jwtToken.Audiences);

        var subClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub || c.Type == ClaimTypes.NameIdentifier);
        Assert.NotNull(subClaim);
        Assert.Equal("42", subClaim.Value);

        var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "name" || c.Type == "unique_name" || c.Type == ClaimTypes.Name);
        Assert.NotNull(nameClaim);
        Assert.Equal("johndoe", nameClaim.Value);

        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == "email");
        Assert.NotNull(emailClaim);
        Assert.Equal("john@example.com", emailClaim.Value);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role");
        Assert.NotNull(roleClaim);
        Assert.Equal("User", roleClaim.Value);
    }

    [Fact]
    public void GenerateAccessToken_MissingKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyKeyOptions = Options.Create(new JwtOptions { Key = "" });
        var service = new TokenService(emptyKeyOptions, NullLogger<TokenService>.Instance);
        var user = new User { Id = 1, Username = "test", Email = "test@example.com" };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => service.GenerateAccessToken(user));
    }

    [Fact]
    public void GenerateRefreshToken_ValidUserId_ReturnsActiveTokenWithCorrectExpiration()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken(10);

        // Assert
        Assert.NotNull(refreshToken);
        Assert.Equal(10, refreshToken.UserId);
        Assert.False(string.IsNullOrWhiteSpace(refreshToken.Token));
        Assert.True(refreshToken.ExpiresAt > DateTime.UtcNow);
        Assert.Null(refreshToken.RevokedAt);
        Assert.True(refreshToken.IsActive);
        Assert.False(refreshToken.IsRevoked);
        Assert.False(refreshToken.IsExpired);
    }

    [Fact]
    public void GetAccessTokenExpirationSeconds_ReturnsExpectedSeconds()
    {
        // Act
        var seconds = _tokenService.GetAccessTokenExpirationSeconds();

        // Assert
        Assert.Equal(30 * 60, seconds);
    }
}
