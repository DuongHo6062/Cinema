using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;

namespace MovieStreaming.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly ApplicationDbContext _context;

    public AdminUserService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<AdminUserDto>> GetUsersAsync(
        string? search,
        int? roleId,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new ValidationException("Page number must be greater than 0.");
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ValidationException("Page size must be between 1 and 100.");
        }

        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(searchLower) ||
                                     u.Email.ToLower().Contains(searchLower));
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => u.RoleId == roleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        query = query.OrderByDescending(u => u.CreatedAt);

        var totalItems = await query.CountAsync(cancellationToken);

        var users = await query
            .Include(u => u.Role)
            .Include(u => u.WatchHistories)
            .Include(u => u.Favorites)
            .Include(u => u.MovieRatings)
            .Include(u => u.MovieComments)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = users.Select(MapToUserDto).ToList();

        return new PagedResult<AdminUserDto>
        {
            Items = items,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDto> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("User ID must be greater than 0.");
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.WatchHistories)
            .Include(u => u.Favorites)
            .Include(u => u.MovieRatings)
            .Include(u => u.MovieComments)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID {id} not found.");
        }

        return MapToUserDto(user);
    }

    public async Task<AdminUserDto> UpdateUserStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("User ID must be greater than 0.");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID {id} not found.");
        }

        // Protect the last active Admin
        if (!isActive && user.IsActive && user.RoleId == 2)
        {
            var activeAdminCount = await _context.Users.CountAsync(u => u.RoleId == 2 && u.IsActive, cancellationToken);
            if (activeAdminCount <= 1)
            {
                throw new ValidationException("Cannot deactivate the last active Admin account.");
            }
        }

        user.IsActive = isActive;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(id, cancellationToken);
    }

    public async Task<AdminUserDto> UpdateUserRoleAsync(int id, int roleId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("User ID must be greater than 0.");
        }

        var roleExists = await _context.Roles.AnyAsync(r => r.Id == roleId, cancellationToken);
        if (!roleExists)
        {
            throw new ValidationException($"Role with ID {roleId} does not exist.");
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException($"User with ID {id} not found.");
        }

        // Protect the last active Admin
        if (user.RoleId == 2 && roleId != 2 && user.IsActive)
        {
            var activeAdminCount = await _context.Users.CountAsync(u => u.RoleId == 2 && u.IsActive, cancellationToken);
            if (activeAdminCount <= 1)
            {
                throw new ValidationException("Cannot remove the Admin role from the last active Admin account.");
            }
        }

        user.RoleId = roleId;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetUserByIdAsync(id, cancellationToken);
    }

    private static AdminUserDto MapToUserDto(User u)
    {
        return new AdminUserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            IsActive = u.IsActive,
            RoleId = u.RoleId,
            RoleName = u.Role != null ? u.Role.Name : "User",
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,
            WatchHistoryCount = u.WatchHistories?.Count ?? 0,
            FavoriteCount = u.Favorites?.Count ?? 0,
            RatingCount = u.MovieRatings?.Count ?? 0,
            CommentCount = u.MovieComments?.Count ?? 0
        };
    }
}
