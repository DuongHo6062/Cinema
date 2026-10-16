using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs.Auth;
using MovieStreaming.Exceptions;
using MovieStreaming.Services.Authentication;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ICurrentUserService? _currentUserService;

    public UsersController(IAuthService authService, ICurrentUserService? currentUserService = null)
    {
        _authService = authService;
        _currentUserService = currentUserService;
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(user);
    }

    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _authService.ChangePasswordAsync(userId, request, cancellationToken);
        return Ok(new { success = true, message = "Password changed successfully." });
    }

    private int GetCurrentUserId()
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated)
        {
            return _currentUserService.UserId;
        }

        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (int.TryParse(claimValue, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedException("User identity is invalid or missing from token.");
    }
}
