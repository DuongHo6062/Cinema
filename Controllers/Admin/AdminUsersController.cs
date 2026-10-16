using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Services.Admin;

namespace MovieStreaming.Controllers.Admin;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _userService;

    public AdminUsersController(IAdminUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get paginated users for admin management.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] int? roleId,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersAsync(search, roleId, isActive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get user details by ID for admin management.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> GetUserById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Activate or deactivate a user account. Protects against deactivating the last active Admin.
    /// </summary>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateUserStatus(
        [FromRoute] int id,
        [FromBody] AdminUpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserStatusAsync(id, request.IsActive, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Change a user's role (1=User, 2=Admin). Protects against removing the last active Admin role.
    /// </summary>
    [HttpPut("{id:int}/role")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateUserRole(
        [FromRoute] int id,
        [FromBody] AdminUpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserRoleAsync(id, request.RoleId, cancellationToken);
        return Ok(result);
    }
}
