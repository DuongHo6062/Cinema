namespace MovieStreaming.DTOs.Episodes;

public class EpisodeDetailDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string? MovieTitle { get; set; }
    public int EpisodeNumber { get; set; }
    public string EpisodeName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int SeasonNumber { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public int? Duration { get; set; }
    public List<EpisodeSourceDto> Sources { get; set; } = new();
}
