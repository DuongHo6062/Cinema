using Microsoft.Extensions.Options;
using MovieStreaming.Configurations;

namespace MovieStreaming.Services.Recommendations;

public class RecommendationScorer : IRecommendationScorer
{
    private readonly RecommendationOptions _options;

    public RecommendationScorer(IOptions<RecommendationOptions> options)
    {
        _options = options.Value ?? new RecommendationOptions();
    }

    public double CalculateGenreSimilarity(IReadOnlyCollection<int> targetGenreIds, IReadOnlyCollection<int> candidateGenreIds)
    {
        if (targetGenreIds == null || targetGenreIds.Count == 0 ||
            candidateGenreIds == null || candidateGenreIds.Count == 0)
        {
            return 0.0;
        }

        var commonCount = targetGenreIds.Intersect(candidateGenreIds).Count();
        if (commonCount == 0) return 0.0;

        var unionCount = targetGenreIds.Union(candidateGenreIds).Count();
        return unionCount > 0 ? (double)commonCount / unionCount : 0.0;
    }

    public double CalculateCategoryMatch(string? targetMovieType, string? candidateMovieType)
    {
        if (string.IsNullOrWhiteSpace(targetMovieType) || string.IsNullOrWhiteSpace(candidateMovieType))
        {
            return 0.0;
        }

        return string.Equals(targetMovieType.Trim(), candidateMovieType.Trim(), StringComparison.OrdinalIgnoreCase)
            ? 1.0
            : 0.0;
    }

    public double CalculateYearProximity(int? targetYear, int? candidateYear)
    {
        if (!targetYear.HasValue || !candidateYear.HasValue)
        {
            return 0.0;
        }

        var diff = Math.Abs(targetYear.Value - candidateYear.Value);
        if (diff == 0) return 1.0;
        if (diff <= 2) return 0.8;
        if (diff <= 5) return 0.5;
        if (diff <= 10) return 0.2;
        return 0.0;
    }

    public double CalculateRatingSignal(decimal? rating)
    {
        if (!rating.HasValue || rating.Value <= 0)
        {
            return 0.0;
        }

        return Math.Clamp((double)rating.Value / 10.0, 0.0, 1.0);
    }

    public double CalculateSimilarScore(
        IReadOnlyCollection<int> targetGenreIds,
        IReadOnlyCollection<int> candidateGenreIds,
        string? targetMovieType,
        string? candidateMovieType,
        int? targetYear,
        int? candidateYear,
        decimal? candidateRating)
    {
        var genreSim = CalculateGenreSimilarity(targetGenreIds, candidateGenreIds);
        var categoryMatch = CalculateCategoryMatch(targetMovieType, candidateMovieType);
        var yearProx = CalculateYearProximity(targetYear, candidateYear);
        var ratingSignal = CalculateRatingSignal(candidateRating);

        var totalWeights = _options.GenreWeight + _options.CategoryWeight + _options.YearWeight + _options.RatingWeight;
        if (totalWeights <= 0) totalWeights = 1.0;

        var score = (genreSim * _options.GenreWeight)
                  + (categoryMatch * _options.CategoryWeight)
                  + (yearProx * _options.YearWeight)
                  + (ratingSignal * _options.RatingWeight);

        var normalized = score / totalWeights;
        return Math.Round(Math.Clamp(normalized, 0.0, 1.0), 4);
    }

    public double CalculatePersonalizedScore(
        IReadOnlyCollection<int> candidateGenreIds,
        string? candidateMovieType,
        int? candidateYear,
        decimal? candidateRating,
        IReadOnlyDictionary<int, double> genrePreferences,
        IReadOnlyDictionary<string, double> typePreferences,
        double maxGenrePref)
    {
        // 1. Genre preference score
        double genreScore = 0.0;
        if (candidateGenreIds != null && candidateGenreIds.Count > 0 && genrePreferences.Count > 0)
        {
            double sum = 0.0;
            foreach (var gId in candidateGenreIds)
            {
                if (genrePreferences.TryGetValue(gId, out var pref))
                {
                    sum += pref;
                }
            }

            if (maxGenrePref > 0)
            {
                genreScore = Math.Clamp(sum / maxGenrePref, -1.0, 1.0);
            }
        }

        // 2. MovieType / Category preference score
        double typeScore = 0.0;
        if (!string.IsNullOrWhiteSpace(candidateMovieType) &&
            typePreferences.TryGetValue(candidateMovieType.Trim(), out var tPref))
        {
            typeScore = tPref > 0 ? 1.0 : (tPref < 0 ? -0.5 : 0.0);
        }

        // 3. Year recency score (closer to current year gives minor boost)
        double yearScore = 0.0;
        if (candidateYear.HasValue)
        {
            var currentYear = DateTime.UtcNow.Year;
            var yearDiff = Math.Max(0, currentYear - candidateYear.Value);
            if (yearDiff <= 1) yearScore = 1.0;
            else if (yearDiff <= 3) yearScore = 0.8;
            else if (yearDiff <= 5) yearScore = 0.5;
            else if (yearDiff <= 10) yearScore = 0.2;
        }

        // 4. Rating signal
        var ratingScore = CalculateRatingSignal(candidateRating);

        // Normalize weights
        var totalWeights = _options.GenreWeight + _options.CategoryWeight + _options.YearWeight + _options.RatingWeight;
        if (totalWeights <= 0) totalWeights = 1.0;

        // Combine signals (negative genreScore reduces overall score)
        var combined = (genreScore * _options.GenreWeight)
                     + (typeScore * _options.CategoryWeight)
                     + (yearScore * _options.YearWeight)
                     + (ratingScore * _options.RatingWeight);

        var normalized = combined / totalWeights;
        return Math.Round(Math.Clamp(normalized, 0.0, 1.0), 4);
    }
}
