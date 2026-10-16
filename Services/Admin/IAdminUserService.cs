using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;

namespace MovieStreaming.Services.Admin;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserDto>> GetUsersAsync(
        string? search,
        int? roleId,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<AdminUserDto> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserRoleAsync(int id, int roleId, CancellationToken cancellationToken = default);
}
