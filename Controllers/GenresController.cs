using Microsoft.AspNetCore.Mvc;
using MovieStreaming.DTOs;
using MovieStreaming.Services;

namespace MovieStreaming.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<GenreDto>>> GetGenres()
    {
        var result = await _genreService.GetAllGenresAsync();
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenreDto>> GetGenreById(int id)
    {
        var result = await _genreService.GetGenreByIdAsync(id);
        return Ok(result);
    }
}
