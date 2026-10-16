namespace MovieStreaming.Models.Entities;

public class WatchHistory
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int MovieId { get; set; }
    public Movie Movie { get; set; } = null!;

    public int EpisodeId { get; set; }
    public Episode Episode { get; set; } = null!;

    public int ProgressSeconds { get; set; }
    public int? DurationSeconds { get; set; }

    public DateTime LastWatchedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
