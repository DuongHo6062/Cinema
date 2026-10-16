namespace MovieStreaming.DTOs.Movies;

public class MovieListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public int? Duration { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? MovieType { get; set; }
    public string? Status { get; set; }
    public string? Quality { get; set; }
    public decimal? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GenreDto> Genres { get; set; } = new();
}
