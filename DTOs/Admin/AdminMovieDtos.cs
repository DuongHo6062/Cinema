using MovieStreaming.DTOs;

namespace MovieStreaming.DTOs.Admin;

public class AdminMovieListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? PosterUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public string? MovieType { get; set; }
    public string? Status { get; set; }
    public string? Quality { get; set; }
    public decimal? Rating { get; set; }
    public int EpisodeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<string> Genres { get; set; } = new List<string>();
}

public class AdminMovieDetailDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? OriginalTitle { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public int? Duration { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? MovieType { get; set; }
    public string? Status { get; set; }
    public string? Quality { get; set; }
    public decimal? Rating { get; set; }
    public string? ExternalId { get; set; }
    public string? ExternalSource { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<AdminGenreDto> Genres { get; set; } = new List<AdminGenreDto>();
    public int EpisodeCount { get; set; }
}

public class AdminCreateMovieRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? OriginalTitle { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public int? Duration { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? MovieType { get; set; }
    public string? Status { get; set; }
    public string? Quality { get; set; }
    public decimal? Rating { get; set; }
    public List<int> GenreIds { get; set; } = new();
}

public class AdminUpdateMovieRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? OriginalTitle { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public int? ReleaseYear { get; set; }
    public int? Duration { get; set; }
    public string? Country { get; set; }
    public string? Language { get; set; }
    public string? MovieType { get; set; }
    public string? Status { get; set; }
    public string? Quality { get; set; }
    public decimal? Rating { get; set; }
    public List<int>? GenreIds { get; set; }
}
