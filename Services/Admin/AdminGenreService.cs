using Microsoft.EntityFrameworkCore;
using MovieStreaming.Data;
using MovieStreaming.DTOs.Admin;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.Common;

namespace MovieStreaming.Services.Admin;

public class AdminGenreService : IAdminGenreService
{
    private readonly ApplicationDbContext _context;

    public AdminGenreService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AdminGenreDto>> GetAllGenresAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new AdminGenreDto
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug,
                Description = g.Description,
                MovieCount = g.MovieGenres.Count
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminGenreDto> GetGenreByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Genre ID must be greater than 0.");
        }

        var genre = await _context.Genres
            .AsNoTracking()
            .Include(g => g.MovieGenres)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (genre == null)
        {
            throw new NotFoundException($"Genre with ID {id} not found.");
        }

        return new AdminGenreDto
        {
            Id = genre.Id,
            Name = genre.Name,
            Slug = genre.Slug,
            Description = genre.Description,
            MovieCount = genre.MovieGenres.Count
        };
    }

    public async Task<AdminGenreDto> CreateGenreAsync(AdminCreateGenreRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Genre name is required.");
        }

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(name)
            : SlugHelper.GenerateSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Invalid slug for genre.");
        }

        var nameExists = await _context.Genres.AnyAsync(g => g.Name.ToLower() == name.ToLower(), cancellationToken);
        if (nameExists)
        {
            throw new ConflictException($"Genre with name '{name}' already exists.");
        }

        var slugExists = await _context.Genres.AnyAsync(g => g.Slug == slug, cancellationToken);
        if (slugExists)
        {
            throw new ConflictException($"Genre with slug '{slug}' already exists.");
        }

        var genre = new Genre
        {
            Name = name,
            Slug = slug,
            Description = request.Description?.Trim()
        };

        await _context.Genres.AddAsync(genre, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new AdminGenreDto
        {
            Id = genre.Id,
            Name = genre.Name,
            Slug = genre.Slug,
            Description = genre.Description,
            MovieCount = 0
        };
    }

    public async Task<AdminGenreDto> UpdateGenreAsync(int id, AdminUpdateGenreRequest request, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Genre ID must be greater than 0.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ValidationException("Genre name is required.");
        }

        var genre = await _context.Genres
            .Include(g => g.MovieGenres)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (genre == null)
        {
            throw new NotFoundException($"Genre with ID {id} not found.");
        }

        var name = request.Name.Trim();
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(name)
            : SlugHelper.GenerateSlug(request.Slug);

        if (name.ToLower() != genre.Name.ToLower())
        {
            var nameExists = await _context.Genres.AnyAsync(g => g.Name.ToLower() == name.ToLower() && g.Id != id, cancellationToken);
            if (nameExists)
            {
                throw new ConflictException($"Genre with name '{name}' already exists.");
            }
        }

        if (slug != genre.Slug)
        {
            var slugExists = await _context.Genres.AnyAsync(g => g.Slug == slug && g.Id != id, cancellationToken);
            if (slugExists)
            {
                throw new ConflictException($"Genre with slug '{slug}' already exists.");
            }
        }

        genre.Name = name;
        genre.Slug = slug;
        genre.Description = request.Description?.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return new AdminGenreDto
        {
            Id = genre.Id,
            Name = genre.Name,
            Slug = genre.Slug,
            Description = genre.Description,
            MovieCount = genre.MovieGenres.Count
        };
    }

    public async Task<bool> DeleteGenreAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ValidationException("Genre ID must be greater than 0.");
        }

        var genre = await _context.Genres
            .Include(g => g.MovieGenres)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (genre == null)
        {
            throw new NotFoundException($"Genre with ID {id} not found.");
        }

        _context.MovieGenres.RemoveRange(genre.MovieGenres);
        _context.Genres.Remove(genre);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
