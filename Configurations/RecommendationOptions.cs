namespace MovieStreaming.Configurations;

public class RecommendationOptions
{
    public const string SectionName = "Recommendation";

    public double GenreWeight { get; set; } = 0.40;
    public double CategoryWeight { get; set; } = 0.20;
    public double YearWeight { get; set; } = 0.10;
    public double UserPreferenceWeight { get; set; } = 0.20;
    public double RatingWeight { get; set; } = 0.10;

    public double WatchHistoryWeight { get; set; } = 1.0;
    public double FavoriteWeight { get; set; } = 2.0;
    public double RatingHighWeight { get; set; } = 2.0;
    public double RatingMediumWeight { get; set; } = 1.0;
    public double RatingLowWeight { get; set; } = -1.0;
}
