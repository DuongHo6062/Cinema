namespace MovieStreaming.DTOs.Movies;

public class MovieQueryParameters
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public string? Genre { get; set; }
    public int? Year { get; set; }
    public string? Type { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? OriginalLanguage { get; set; }
    public bool? IsVietsub { get; set; }
    public string? SortBy { get; set; } = "newest";
}
