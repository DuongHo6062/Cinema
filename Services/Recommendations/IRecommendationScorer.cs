namespace MovieStreaming.Services.Recommendations;

public interface IRecommendationScorer
{
    double CalculateGenreSimilarity(IReadOnlyCollection<int> targetGenreIds, IReadOnlyCollection<int> candidateGenreIds);
    double CalculateCategoryMatch(string? targetMovieType, string? candidateMovieType);
    double CalculateYearProximity(int? targetYear, int? candidateYear);
    double CalculateRatingSignal(decimal? rating);
    double CalculateSimilarScore(
        IReadOnlyCollection<int> targetGenreIds,
        IReadOnlyCollection<int> candidateGenreIds,
        string? targetMovieType,
        string? candidateMovieType,
        int? targetYear,
        int? candidateYear,
        decimal? candidateRating);
    double CalculatePersonalizedScore(
        IReadOnlyCollection<int> candidateGenreIds,
        string? candidateMovieType,
        int? candidateYear,
        decimal? candidateRating,
        IReadOnlyDictionary<int, double> genrePreferences,
        IReadOnlyDictionary<string, double> typePreferences,
        double maxGenrePref);
}
