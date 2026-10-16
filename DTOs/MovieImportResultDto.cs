namespace MovieStreaming.DTOs;

public class MovieImportResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? MovieId { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public bool IsNew { get; set; }
    public int EpisodesImported { get; set; }
    public int SourcesImported { get; set; }
    public int GenresImported { get; set; }
}
