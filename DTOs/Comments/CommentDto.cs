namespace MovieStreaming.DTOs.Comments;

public class CommentDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsOwner { get; set; }
}
