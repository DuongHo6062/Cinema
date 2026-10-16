namespace MovieStreaming.Models.Entities;

public class Movie
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
    public string? ExternalId { get; set; }
    public string? ExternalSource { get; set; }
    public string? OriginalLanguage { get; set; }
    public bool IsVietsub { get; set; } = false;
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<Episode> Episodes { get; set; } = new List<Episode>();
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<MovieRating> MovieRatings { get; set; } = new List<MovieRating>();
    public ICollection<MovieComment> MovieComments { get; set; } = new List<MovieComment>();
}
