namespace MovieStreaming.DTOs;

public class EpisodeDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int EpisodeNumber { get; set; }
    public string EpisodeName { get; set; } = string.Empty;
    public int SeasonNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int? Duration { get; set; }
    public List<EpisodeSourceDto> Sources { get; set; } = new();
}
