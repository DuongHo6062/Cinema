namespace MovieStreaming.DTOs.Ratings;

public class RatingSummaryDto
{
    public int MovieId { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<string, int> Distribution { get; set; } = new();
}
