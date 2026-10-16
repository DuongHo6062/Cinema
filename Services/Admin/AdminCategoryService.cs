using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Common;

namespace MovieStreaming.Services.Admin;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly ApplicationDbContext _context;

    public AdminCategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminCategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                Description = c.Description
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminCategoryDto> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Category ID must be greater than 0.");
        }

        var cat = await _context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cat == null)
        {
            throw new NotFoundException($"Category with ID {id} not found.");
        }

        return new AdminCategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Slug = cat.Slug,
            Description = cat.Description
        };
    }

    public async Task<AdminCategoryDto> CreateCategoryAsync(AdminCreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(name)
            : SlugHelper.GenerateSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Invalid slug for category.");
        }

        var nameExists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower(), cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"Category with name '{name}' already exists.");
        }

        var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);
        if (slugExists)
        {
            throw new ConflictException($"Category with slug '{slug}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Slug = slug,
            Description = request.Description?.Trim()
        };

        await _context.Categories.AddAsync(category, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AdminCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description
        };
    }

    public async Task<AdminCategoryDto> UpdateCategoryAsync(int id, AdminUpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Category ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Category name is required.");
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException($"Category with ID {id} not found.");
        }

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(name)
            : SlugHelper.GenerateSlug(request.Slug);

        if (name.ToLower() != category.Name.ToLower())
        {
            var nameExists = await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != id, cancellationToken);
            if (nameExists)
            {
                throw new ConflictException($"Category with name '{name}' already exists.");
            }
        }

        if (slug != category.Slug)
        {
            var slugExists = await _context.Categories.AnyAsync(c => c.Slug == slug && c.Id != id, cancellationToken);
            if (slugExists)
            {
                throw new ConflictException($"Category with slug '{slug}' already exists.");
            }
        }

        category.Name = name;
        category.Slug = slug;
        category.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new AdminCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description
        };
    }

    public async Task<bool> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Category ID must be greater than 0.");
        }

        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException($"Category with ID {id} not found.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
