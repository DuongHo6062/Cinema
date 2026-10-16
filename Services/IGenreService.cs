using MovieStreaming.DTOs;

namespace MovieStreaming.Services;

public interface IGenreService
{
    Task<IEnumerable<GenreDto>> GetAllGenresAsync();
    Task<GenreDto> GetGenreByIdAsync(int id);
}
