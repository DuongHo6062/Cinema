namespace MovieStreaming.DTOs.Admin;

public class AdminDashboardDto
{
    public int TotalMovies { get; set; }
    public int TotalUsers { get; set; }
    public int TotalGenres { get; set; }
    public int TotalCategories { get; set; }
    public int TotalEpisodes { get; set; }
    public int TotalComments { get; set; }
    public int TotalRatings { get; set; }
    public int TotalFavorites { get; set; }
    public int TotalWatchHistory { get; set; }
}

public class AdminRecentActivityDto
{
    public IReadOnlyList<AdminRecentUserDto> RecentUsers { get; set; } = new List<AdminRecentUserDto>();
    public IReadOnlyList<AdminRecentMovieDto> RecentMovies { get; set; } = new List<AdminRecentMovieDto>();
    public IReadOnlyList<AdminRecentCommentDto> RecentComments { get; set; } = new List<AdminRecentCommentDto>();
    public IReadOnlyList<AdminRecentRatingDto> RecentRatings { get; set; } = new List<AdminRecentRatingDto>();
}

public class AdminRecentUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminRecentMovieDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? PosterUrl { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminRecentCommentDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AdminRecentRatingDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public string MovieTitle { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
}
