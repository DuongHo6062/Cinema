namespace MovieStreaming.DTOs.Ratings;

public class RatingDto
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
