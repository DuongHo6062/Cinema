namespace MovieStreaming.DTOs.Recommendations;

public class RecommendationMovieDto
{
    public int MovieId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public decimal? Rating { get; set; }
    public string? MovieType { get; set; }
    public List<string> Genres { get; set; } = new();
    public double Score { get; set; }
    public string? Reason { get; set; }
}

public class RecommendationQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool ExcludeConsumed { get; set; } = true;
}
