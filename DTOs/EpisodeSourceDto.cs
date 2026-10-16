namespace MovieStreaming.DTOs;

public class EpisodeSourceDto
{
    public int Id { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? SourceType { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? EmbedUrl { get; set; }
}
