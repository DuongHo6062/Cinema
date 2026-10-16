using MovieStreaming.Models.Entities;

namespace MovieStreaming.Repositories;

public interface IGenreRepository
{
    Task<IEnumerable<Genre>> GetAllGenresAsync();
    Task<Genre?> GetGenreByIdAsync(int id);
}
