namespace MovieStreaming.DTOs.Favorites;

public class FavoriteMovieDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public string? MovieType { get; set; }
    public string? Quality { get; set; }
    public decimal? Rating { get; set; }
    public DateTime FavoritedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
