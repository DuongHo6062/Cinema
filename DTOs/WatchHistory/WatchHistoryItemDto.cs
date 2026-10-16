namespace MovieStreaming.DTOs.WatchHistory;

public class WatchHistoryItemDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public string MovieSlug { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public int EpisodeId { get; set; }
    public int EpisodeNumber { get; set; }
    public string EpisodeName { get; set; } = string.Empty;
    public int SeasonNumber { get; set; }
    public int ProgressSeconds { get; set; }
    public int? DurationSeconds { get; set; }
    public double ProgressPercentage { get; set; }
    public bool Completed { get; set; }
    public DateTime LastWatchedAt { get; set; }
}
