namespace MovieStreaming.Models.Entities;

public class EpisodeSource
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public Episode Episode { get; set; } = null!;

    public string ServerName { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? SourceType { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? EmbedUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
