namespace MovieStreaming.Models.Entities;

public class Episode
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; } = 1;
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int? Duration { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<EpisodeSource> Sources { get; set; } = new List<EpisodeSource>();
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
}
