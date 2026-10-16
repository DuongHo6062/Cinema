namespace MovieStreaming.DTOs.Episodes;

public class EpisodeSourcesResponseDto
{
    public int EpisodeId { get; set; }
    public List<EpisodeSourceDto> Sources { get; set; } = new();
}
