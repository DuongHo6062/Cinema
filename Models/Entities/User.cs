namespace MovieStreaming.Models.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int RoleId { get; set; } = 1;
    public Role Role { get; set; } = null!;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<WatchHistory> WatchHistories { get; set; } = new List<WatchHistory>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<MovieRating> MovieRatings { get; set; } = new List<MovieRating>();
    public ICollection<MovieComment> MovieComments { get; set; } = new List<MovieComment>();
}
