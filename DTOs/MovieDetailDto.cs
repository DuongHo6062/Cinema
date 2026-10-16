namespace MovieStreaming.DTOs;

public class MovieDetailDto : MovieDto
{
    public DateTime UpdatedAt { get; set; }
    public List<EpisodeDto> Episodes { get; set; } = new();
}
