using MovieStreaming.DTOs;
using MovieStreaming.Exceptions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Repositories;

namespace MovieStreaming.Services;

public class GenreService : IGenreService
{
    private readonly IGenreRepository _genreRepository;

    public GenreService(IGenreRepository genreRepository)
    {
        _genreRepository = genreRepository;
    }

    public async Task<IEnumerable<GenreDto>> GetAllGenresAsync()
    {
        var genres = await _genreRepository.GetAllGenresAsync();
        return genres.Select(MapToGenreDto);
    }

    public async Task<GenreDto> GetGenreByIdAsync(int id)
    {
        if (id <= 0)
        {
            throw new ValidationException("Genre ID must be greater than 0.");
        }

        var genre = await _genreRepository.GetGenreByIdAsync(id);
        if (genre == null)
        {
            throw new NotFoundException($"Genre with ID {id} not found.");
        }

        return MapToGenreDto(genre);
    }

    private static GenreDto MapToGenreDto(Genre genre)
    {
        return new GenreDto
        {
            Id = genre.Id,
            Name = genre.Name,
            Slug = genre.Slug,
            Description = genre.Description
        };
    }
}
