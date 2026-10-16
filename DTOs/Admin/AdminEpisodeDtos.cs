namespace MovieStreaming.DTOs.Admin;

public class AdminEpisodeDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int? Duration { get; set; }
    public int SourceCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public IReadOnlyList<AdminEpisodeSourceDto> Sources { get; set; } = new List<AdminEpisodeSourceDto>();
}

public class AdminCreateEpisodeRequest
{
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; } = 1;
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int? Duration { get; set; }
}

public class AdminUpdateEpisodeRequest
{
    public int EpisodeNumber { get; set; }
    public int SeasonNumber { get; set; } = 1;
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public int? DisplayOrder { get; set; }
    public string? Description { get; set; }
    public string? VideoUrl { get; set; }
    public int? Duration { get; set; }
}

public class AdminEpisodeSourceDto
{
    public int Id { get; set; }
    public int EpisodeId { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? SourceType { get; set; }
    public string VideoUrl { get; set; } = string.Empty;
    public string? EmbedUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdminCreateEpisodeSourceRequest
{
    public string ServerName { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? SourceType { get; set; } = "HLS";
    public string VideoUrl { get; set; } = string.Empty;
    public string? EmbedUrl { get; set; }
}

public class AdminUpdateEpisodeSourceRequest
{
    public string ServerName { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? SourceType { get; set; } = "HLS";
    public string VideoUrl { get; set; } = string.Empty;
    public string? EmbedUrl { get; set; }
}
