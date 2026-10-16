using System.Net.Mail;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Auth;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Services.Authentication;

public class AuthService : IAuthService
{
    private static readonly Regex UsernameRegex = new(@"^[a-zA-Z0-9_-]+$", RegexOptions.Compiled);
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext context,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRegisterRequest(request);

        var normalizedUsername = request.Username.Trim();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername.ToLower() || u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (existingUser != null)
        {
            if (string.Equals(existingUser.Username, normalizedUsername, StringComparison.OrdinalIgnoreCase))
            {
                throw new ConflictException("Username is already taken.");
            }

            throw new ConflictException("Email is already registered.");
        }

        var userRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

        var user = new User
        {
            Username = normalizedUsername,
            Email = normalizedEmail,
            RoleId = userRole?.Id ?? 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User '{Username}' registered successfully with ID {UserId}", user.Username, user.Id);

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = userRole?.Name ?? "User",
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Username/email and password are required.");
        }

        var identifier = request.UsernameOrEmail.Trim();

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username.ToLower() == identifier.ToLower() || u.Email.ToLower() == identifier.ToLower(), cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User '{Identifier}' not found", identifier);
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login failed: User '{Username}' (ID {UserId}) is deactivated", user.Username, user.Id);
            throw new UnauthorizedException("Account is deactivated. Please contact support.");
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: Invalid password for user '{Username}'", user.Username);
            throw new UnauthorizedException("Invalid username/email or password.");
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User '{Username}' (ID {UserId}) logged in successfully", user.Username, user.Id);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn = _tokenService.GetAccessTokenExpirationSeconds(),
            User = new UserResponse
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role?.Name ?? "User",
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ValidationException("Refresh token is required.");
        }

        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

        if (storedToken == null || storedToken.IsRevoked || storedToken.IsExpired)
        {
            _logger.LogWarning("Refresh token failure: Invalid, revoked, or expired token");
            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        if (!storedToken.User.IsActive)
        {
            _logger.LogWarning("Refresh token failure: User '{Username}' is deactivated", storedToken.User.Username);
            throw new UnauthorizedException("Account is deactivated.");
        }

        // Refresh token rotation: revoke old token and issue a new one
        storedToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = _tokenService.GenerateRefreshToken(storedToken.UserId);
        var newAccessToken = _tokenService.GenerateAccessToken(storedToken.User);

        _context.RefreshTokens.Add(newRefreshToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token rotated successfully for user '{Username}' (ID {UserId})", storedToken.User.Username, storedToken.UserId);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresIn = _tokenService.GetAccessTokenExpirationSeconds(),
            User = new UserResponse
            {
                Id = storedToken.User.Id,
                Username = storedToken.User.Username,
                Email = storedToken.User.Email,
                Role = storedToken.User.Role?.Name ?? "User",
                CreatedAt = storedToken.User.CreatedAt
            }
        };
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && rt.RevokedAt == null, cancellationToken);

        if (storedToken != null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Refresh token revoked for user ID {UserId}", storedToken.UserId);
        }
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            throw new ValidationException("Current password is required.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException("New password is required.");
        }

        if (request.NewPassword.Length < 6)
        {
            throw new ValidationException("New password must be at least 6 characters long.");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            throw new ValidationException("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke active refresh tokens for enhanced security
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Password changed successfully for user '{Username}' (ID {UserId})", user.Username, user.Id);
    }

    public async Task<UserResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
        {
            throw new UnauthorizedException("User is not authenticated.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User not found.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedException("Account is deactivated.");
        }

        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role?.Name ?? "User",
            CreatedAt = user.CreatedAt
        };
    }

    private static void ValidateRegisterRequest(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            throw new ValidationException("Username is required.");
        }

        var trimmedUsername = request.Username.Trim();
        if (trimmedUsername.Length < 3 || trimmedUsername.Length > 50)
        {
            throw new ValidationException("Username must be between 3 and 50 characters.");
        }

        if (!UsernameRegex.IsMatch(trimmedUsername))
        {
            throw new ValidationException("Username can only contain alphanumeric characters, underscores, and hyphens.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required.");
        }

        if (!IsValidEmail(request.Email.Trim()))
        {
            throw new ValidationException("Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Password is required.");
        }

        if (request.Password.Length < 6)
        {
            throw new ValidationException("Password must be at least 6 characters long.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var mailAddress = new MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
