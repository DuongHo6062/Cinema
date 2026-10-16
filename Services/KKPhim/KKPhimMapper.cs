using System.Text.RegularExpressions;
using MovieStreaming.Models.Entities;
using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Services.KKPhim;

public static class KKPhimMapper
{
    private const string DefaultCdnDomain = "https://phimimg.com";

    public static Movie MapToMovie(KKPhimMovieDetailItem item, Movie? existingMovie = null)
    {
        var movie = existingMovie ?? new Movie();

        movie.Title = item.Name.Trim();
        movie.Slug = item.Slug.Trim();
        movie.OriginalTitle = item.OriginName?.Trim();
        movie.Description = StripHtml(item.Content);
        movie.PosterUrl = NormalizeImageUrl(item.PosterUrl);
        movie.ThumbnailUrl = NormalizeImageUrl(item.ThumbUrl);
        movie.ReleaseYear = item.Year;
        movie.Duration = ParseDurationMinutes(item.Time);
        movie.Country = FormatCountries(item.Country);
        movie.Language = item.Lang?.Trim();
        movie.OriginalLanguage = KKPhimFilterHelper.IsEnglishMovie(item) ? "English" : (movie.OriginalLanguage ?? null);
        movie.IsVietsub = item.Lang != null && item.Lang.Contains("Vietsub", StringComparison.OrdinalIgnoreCase);
        movie.LastSyncedAt = DateTime.UtcNow;
        movie.MovieType = item.Type?.Trim();
        movie.Status = item.Status?.Trim();
        movie.Quality = item.Quality?.Trim();
        movie.Rating = item.Tmdb?.VoteAverage;
        movie.ExternalId = item.Id.Trim();
        movie.ExternalSource = "KKPHIM";
        movie.UpdatedAt = DateTime.UtcNow;

        if (existingMovie == null)
        {
            movie.CreatedAt = DateTime.UtcNow;
        }

        return movie;
    }

    public static int ParseEpisodeNumber(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return 1;
        }

        if (name.Equals("Full", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        var match = Regex.Match(name, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var number))
        {
            return number;
        }

        return 1;
    }

    public static int? ParseDurationMinutes(string? time)
    {
        if (string.IsNullOrWhiteSpace(time))
        {
            return null;
        }

        var match = Regex.Match(time, @"(\d+)\s*(?:phút|min|m)", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var minutes))
        {
            return minutes;
        }

        // Fallback to first digits if any
        var fallbackMatch = Regex.Match(time, @"\d+");
        if (fallbackMatch.Success && int.TryParse(fallbackMatch.Value, out var fallbackMinutes))
        {
            return fallbackMinutes;
        }

        return null;
    }

    public static string? NormalizeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        url = url.Trim();
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (url.StartsWith("/"))
        {
            return $"{DefaultCdnDomain}{url}";
        }

        return $"{DefaultCdnDomain}/{url}";
    }

    public static string? FormatCountries(List<KKPhimCountryItem>? countries)
    {
        if (countries == null || countries.Count == 0)
        {
            return null;
        }

        return string.Join(", ", countries.Select(c => c.Name.Trim()).Where(n => !string.IsNullOrEmpty(n)));
    }

    public static string? StripHtml(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var stripped = Regex.Replace(input, "<.*?>", string.Empty);
        return System.Net.WebUtility.HtmlDecode(stripped).Trim();
    }
}
