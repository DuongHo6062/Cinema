namespace MovieStreaming.DTOs;

public class KKPhimSearchItemDto
{
    public string ExternalId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? MovieType { get; set; }
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? EpisodeCurrent { get; set; }
    public string? Quality { get; set; }
    public string? Language { get; set; }
    public int? Year { get; set; }
    public List<string> Genres { get; set; } = new();
    public List<string> Countries { get; set; } = new();
}

public class KKPhimSearchResultDto
{
    public List<KKPhimSearchItemDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}
