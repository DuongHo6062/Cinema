using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MovieStreaming.Configurations;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Auth;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly AuthService _authService;

    private class TestApplicationDbContext : ApplicationDbContext
    {
        public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entity.GetProperties())
                {
                    if (property.GetColumnType() == "nvarchar(max)")
                    {
                        property.SetColumnType("TEXT");
                    }
                }
                foreach (var index in entity.GetIndexes())
                {
                    index.SetFilter(null);
                }
            }
        }
    }

    public AuthServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestApplicationDbContext(options);
        _context.Database.EnsureCreated();

        _passwordHasher = new PasswordHasher<User>();

        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "MovieStreamingTest",
            Audience = "MovieStreamingClientTest",
            Key = "TestSecretKeyThatIsAtLeast32BytesLongForHmacSha256Security12345!",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        });

        _tokenService = new TokenService(jwtOptions, NullLogger<TokenService>.Instance);

        _authService = new AuthService(
            _context,
            _passwordHasher,
            _tokenService,
            NullLogger<AuthService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ==================== Registration Tests ====================

    [Fact]
    public async Task RegisterAsync_ValidRequest_CreatesUserWithDefaultRoleAndHashedPassword()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _authService.RegisterAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("testuser", response.Username);
        Assert.Equal("testuser@example.com", response.Email);
        Assert.Equal("User", response.Role);

        var userInDb = await _context.Users.FirstOrDefaultAsync(u => u.Username == "testuser");
        Assert.NotNull(userInDb);
        Assert.True(userInDb.IsActive);
        Assert.NotEqual("Password123!", userInDb.PasswordHash);

        var verifyResult = _passwordHasher.VerifyHashedPassword(userInDb, userInDb.PasswordHash, "Password123!");
        Assert.Equal(PasswordVerificationResult.Success, verifyResult);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateUsername_ThrowsConflictException()
    {
        // Arrange: Pre-populate user
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "existinguser",
            Email = "existing@example.com",
            Password = "Password123!"
        });

        // Act & Assert
        var duplicateRequest = new RegisterRequest
        {
            Username = "EXISTINGUSER", // case-insensitive check
            Email = "other@example.com",
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(duplicateRequest));
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ThrowsConflictException()
    {
        // Arrange: Pre-populate user
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "userone",
            Email = "duplicate@example.com",
            Password = "Password123!"
        });

        // Act & Assert
        var duplicateRequest = new RegisterRequest
        {
            Username = "usertwo",
            Email = "DUPLICATE@example.com", // case-insensitive check
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<ConflictException>(() => _authService.RegisterAsync(duplicateRequest));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@missinguser.com")]
    [InlineData("missingdomain@")]
    public async Task RegisterAsync_InvalidEmail_ThrowsValidationException(string invalidEmail)
    {
        var request = new RegisterRequest
        {
            Username = "validuser",
            Email = invalidEmail,
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _authService.RegisterAsync(request));
    }

    [Theory]
    [InlineData("12345")] // less than 6 chars
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegisterAsync_ShortPassword_ThrowsValidationException(string shortPassword)
    {
        var request = new RegisterRequest
        {
            Username = "validuser",
            Email = "valid@example.com",
            Password = shortPassword
        };

        await Assert.ThrowsAsync<ValidationException>(() => _authService.RegisterAsync(request));
    }

    [Theory]
    [InlineData("ab")] // too short (< 3 chars)
    [InlineData("user with spaces")]
    [InlineData("user@special#")]
    public async Task RegisterAsync_InvalidUsernameCharacters_ThrowsValidationException(string invalidUsername)
    {
        var request = new RegisterRequest
        {
            Username = invalidUsername,
            Email = "valid@example.com",
            Password = "Password123!"
        };

        await Assert.ThrowsAsync<ValidationException>(() => _authService.RegisterAsync(request));
    }

    // ==================== Login Tests ====================

    [Fact]
    public async Task LoginAsync_ValidUsername_ReturnsTokensAndUserInfo()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "loginuser",
            Email = "loginuser@example.com",
            Password = "CorrectPassword123!"
        });

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "loginuser",
            Password = "CorrectPassword123!"
        };

        // Act
        var response = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("loginuser", response.User.Username);
        Assert.Equal("loginuser@example.com", response.User.Email);
        Assert.Equal("User", response.User.Role);

        // Verify refresh token in database
        var tokenInDb = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == response.RefreshToken);
        Assert.NotNull(tokenInDb);
        Assert.True(tokenInDb.IsActive);
    }

    [Fact]
    public async Task LoginAsync_ValidEmail_ReturnsTokensAndUserInfo()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "emailuser",
            Email = "emailuser@example.com",
            Password = "CorrectPassword123!"
        });

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "emailuser@example.com",
            Password = "CorrectPassword123!"
        };

        // Act
        var response = await _authService.LoginAsync(loginRequest);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("emailuser", response.User.Username);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedException()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "wrongpassuser",
            Email = "wrongpass@example.com",
            Password = "CorrectPassword123!"
        });

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "wrongpassuser",
            Password = "WrongPassword999!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(loginRequest));
        Assert.Equal("Invalid username/email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_UnknownUser_ThrowsUnauthorizedException()
    {
        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "nonexistentuser",
            Password = "Password123!"
        };

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(loginRequest));
        Assert.Equal("Invalid username/email or password.", ex.Message);
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ThrowsUnauthorizedException()
    {
        // Arrange: Register user then deactivate
        var userResponse = await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "inactiveuser",
            Email = "inactive@example.com",
            Password = "Password123!"
        });

        var user = await _context.Users.FindAsync(userResponse.Id);
        user!.IsActive = false;
        await _context.SaveChangesAsync();

        var loginRequest = new LoginRequest
        {
            UsernameOrEmail = "inactiveuser",
            Password = "Password123!"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(loginRequest));
        Assert.Contains("deactivated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // ==================== Refresh Token Tests ====================

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesTokenAndReturnsNewTokens()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "refreshuser",
            Email = "refresh@example.com",
            Password = "Password123!"
        });

        var loginResponse = await _authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "refreshuser",
            Password = "Password123!"
        });

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResponse.RefreshToken
        };

        // Act
        var refreshResponse = await _authService.RefreshTokenAsync(refreshRequest);

        // Assert
        Assert.NotNull(refreshResponse);
        Assert.False(string.IsNullOrWhiteSpace(refreshResponse.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshResponse.RefreshToken));
        Assert.NotEqual(loginResponse.RefreshToken, refreshResponse.RefreshToken);

        // Verify old token is marked revoked
        var oldToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == loginResponse.RefreshToken);
        Assert.NotNull(oldToken);
        Assert.True(oldToken.IsRevoked);

        // Verify new token is active
        var newToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshResponse.RefreshToken);
        Assert.NotNull(newToken);
        Assert.True(newToken.IsActive);
    }

    [Fact]
    public async Task RefreshTokenAsync_RevokedToken_ThrowsUnauthorizedException()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "revokeduser",
            Email = "revoked@example.com",
            Password = "Password123!"
        });

        var loginResponse = await _authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "revokeduser",
            Password = "Password123!"
        });

        // First refresh rotates token
        await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResponse.RefreshToken });

        // Act & Assert: Attempting to use old revoked token again must fail
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResponse.RefreshToken }));
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ThrowsUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            Username = "expireduser",
            Email = "expired@example.com",
            RoleId = 1,
            IsActive = true
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var expiredToken = new RefreshToken
        {
            UserId = user.Id,
            Token = "expired-token-string",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.RefreshTokens.Add(expiredToken);
        await _context.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = "expired-token-string" }));
    }

    // ==================== Logout Tests ====================

    [Fact]
    public async Task LogoutAsync_ValidToken_RevokesToken()
    {
        // Arrange
        await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "logoutuser",
            Email = "logout@example.com",
            Password = "Password123!"
        });

        var loginResponse = await _authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "logoutuser",
            Password = "Password123!"
        });

        // Act
        await _authService.LogoutAsync(new LogoutRequest { RefreshToken = loginResponse.RefreshToken });

        // Assert: Token is revoked
        var tokenInDb = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == loginResponse.RefreshToken);
        Assert.NotNull(tokenInDb);
        Assert.True(tokenInDb.IsRevoked);

        // Attempting to refresh after logout must fail
        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = loginResponse.RefreshToken }));
    }

    // ==================== Change Password Tests ====================

    [Fact]
    public async Task ChangePasswordAsync_CorrectCurrentPassword_UpdatesPasswordAndRevokesRefreshTokens()
    {
        // Arrange
        var registerResponse = await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "changepassuser",
            Email = "changepass@example.com",
            Password = "OldPassword123!"
        });

        var loginResponse = await _authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "changepassuser",
            Password = "OldPassword123!"
        });

        // Act
        await _authService.ChangePasswordAsync(registerResponse.Id, new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "BrandNewPassword456!"
        });

        // Assert 1: Old password fails
        await Assert.ThrowsAsync<UnauthorizedException>(() => _authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "changepassuser",
            Password = "OldPassword123!"
        }));

        // Assert 2: New password succeeds
        var newLoginResponse = await _authService.LoginAsync(new LoginRequest
        {
            UsernameOrEmail = "changepassuser",
            Password = "BrandNewPassword456!"
        });
        Assert.NotNull(newLoginResponse);

        // Assert 3: Previous refresh token revoked
        var oldToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == loginResponse.RefreshToken);
        Assert.NotNull(oldToken);
        Assert.True(oldToken.IsRevoked);
    }

    [Fact]
    public async Task ChangePasswordAsync_IncorrectCurrentPassword_ThrowsValidationException()
    {
        // Arrange
        var registerResponse = await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "wrongpasschange",
            Email = "wrongchange@example.com",
            Password = "OldPassword123!"
        });

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _authService.ChangePasswordAsync(registerResponse.Id, new ChangePasswordRequest
        {
            CurrentPassword = "WrongCurrentPassword!",
            NewPassword = "BrandNewPassword456!"
        }));
    }

    // ==================== Get Current User Tests ====================

    [Fact]
    public async Task GetCurrentUserAsync_ValidUserId_ReturnsUserResponse()
    {
        // Arrange
        var registerResponse = await _authService.RegisterAsync(new RegisterRequest
        {
            Username = "profileuser",
            Email = "profile@example.com",
            Password = "Password123!"
        });

        // Act
        var user = await _authService.GetCurrentUserAsync(registerResponse.Id);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(registerResponse.Id, user.Id);
        Assert.Equal("profileuser", user.Username);
        Assert.Equal("profile@example.com", user.Email);
        Assert.Equal("User", user.Role);
    }

    [Fact]
    public async Task GetCurrentUserAsync_NotFound_ThrowsNotFoundException()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _authService.GetCurrentUserAsync(99999));
    }
}
