using System.Text.RegularExpressions;
using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Services.KKPhim;

public static class KKPhimFilterHelper
{
    // Primary English-speaking countries and Western film industry designations used in Vietnamese film databases
    private static readonly HashSet<string> EnglishCountrySlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "au-my",
        "my",
        "anh",
        "canada",
        "uc",
        "new-zealand",
        "ireland",
        "ai-len"
    };

    private static readonly string[] EnglishCountryNames =
    {
        "Âu Mỹ", "Au My", "Mỹ", "Hoa Kỳ", "United States", "US", "USA",
        "Anh", "Vương quốc Anh", "UK", "United Kingdom", "Great Britain",
        "Canada",
        "Úc", "Australia",
        "New Zealand",
        "Ireland", "Ai-len"
    };

    // Strictly non-English countries in Vietnamese databases
    private static readonly string[] StrictlyNonEnglishCountries =
    {
        "Hàn Quốc", "Trung Quốc", "Nhật Bản", "Thái Lan", "Việt Nam",
        "Đài Loan", "Hồng Kông", "Ấn Độ", "Indonesia", "Philippines",
        "Malaysia", "Nga", "Tây Ban Nha", "Thổ Nhĩ Kỳ", "Pháp", "Đức", "Ý"
    };

    // Regex to detect non-Latin scripts (Korean Hangul, Chinese Hanzi, Japanese Kana, Thai, Cyrillic, Arabic, Devanagari)
    private static readonly Regex NonLatinScriptRegex = new(
        @"[\uAC00-\uD7AF\u1100-\u11FF\u4E00-\u9FFF\u3040-\u30FF\u0E00-\u0E7F\u0400-\u04FF\u0600-\u06FF\u0900-\u097F]",
        RegexOptions.Compiled);

    /// <summary>
    /// Evaluates if a movie is from an English-language territory / origin.
    /// Considers both country metadata and original title script.
    /// </summary>
    public static bool IsEnglishMovie(KKPhimMovieDetailItem movie)
    {
        if (movie == null) return false;

        var countryList = movie.Country ?? new List<KKPhimCountryItem>();

        // 1. Check if any country matches English-speaking / Western countries
        var hasEnglishCountry = countryList.Any(c =>
            EnglishCountrySlugs.Contains(c.Slug) ||
            EnglishCountryNames.Any(name => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)));

        // 2. Check if all countries are strictly non-English
        var hasStrictlyNonEnglish = countryList.Any(c =>
            StrictlyNonEnglishCountries.Any(name => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)));

        if (hasStrictlyNonEnglish && !hasEnglishCountry)
        {
            return false;
        }

        // 3. Inspect origin name for non-Latin scripts (e.g. Hangul, Hanzi, Thai)
        if (!string.IsNullOrWhiteSpace(movie.OriginName) && NonLatinScriptRegex.IsMatch(movie.OriginName))
        {
            // If the original title is written in Asian/Cyrillic/Arabic script and there's no English country, reject
            if (!hasEnglishCountry)
            {
                return false;
            }
        }

        // 4. Default: If it matches English countries (e.g. au-my, anh, my, uc, canada), accept
        return hasEnglishCountry;
    }

    /// <summary>
    /// Quick pre-filter for search/list items before calling the detail API.
    /// </summary>
    public static bool QuickPassesEnglishPreFilter(KKPhimSearchMovieItem item)
    {
        if (item == null) return false;

        var countryList = item.Country ?? new List<KKPhimCountryItem>();
        if (countryList.Count == 0)
        {
            // If country is not provided in list API, allow detail lookup to check
            return true;
        }

        var hasEnglishCountry = countryList.Any(c =>
            EnglishCountrySlugs.Contains(c.Slug) ||
            EnglishCountryNames.Any(name => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)));

        var hasStrictlyNonEnglish = countryList.Any(c =>
            StrictlyNonEnglishCountries.Any(name => c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)));

        if (hasStrictlyNonEnglish && !hasEnglishCountry)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(item.OriginName) && NonLatinScriptRegex.IsMatch(item.OriginName))
        {
            if (!hasEnglishCountry)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluates if a movie has a Vietsub (Vietnamese subtitles) version available.
    /// Rejects if the movie ONLY has Thuyết Minh (voiceover) or Lồng Tiếng (dubbed).
    /// </summary>
    public static bool HasVietsub(KKPhimMovieDetailItem movie, List<KKPhimServerItem>? episodes)
    {
        if (movie == null) return false;

        // 1. Check movie language metadata
        var lang = movie.Lang?.ToLowerInvariant() ?? string.Empty;

        // If lang only contains thuyết minh or lồng tiếng without vietsub, reject
        var isDubbedOnly = (lang.Contains("thuyết minh") || lang.Contains("thuyet minh") || lang.Contains("lồng tiếng") || lang.Contains("long tieng"))
                           && !lang.Contains("vietsub");

        if (isDubbedOnly)
        {
            return false;
        }

        // 2. Check episodes and servers
        if (episodes != null && episodes.Count > 0)
        {
            var hasVietsubServer = episodes.Any(server => IsVietsubServer(server.ServerName));
            if (!hasVietsubServer)
            {
                // All servers are explicitly dubbed or non-vietsub
                return false;
            }
        }

        // 3. Must indicate vietsub either in movie.Lang or in at least one episode server
        var langIndicatesVietsub = lang.Contains("vietsub") || string.IsNullOrWhiteSpace(lang);
        var serverIndicatesVietsub = episodes == null || episodes.Count == 0 || episodes.Any(server => IsVietsubServer(server.ServerName));

        return langIndicatesVietsub && serverIndicatesVietsub;
    }

    /// <summary>
    /// Quick pre-filter for search/list items based on language tag.
    /// </summary>
    public static bool QuickPassesVietsubPreFilter(KKPhimSearchMovieItem item)
    {
        if (item == null) return false;

        var lang = item.Lang?.ToLowerInvariant() ?? string.Empty;

        // If explicitly only dubbed, reject
        var isDubbedOnly = (lang.Contains("thuyết minh") || lang.Contains("thuyet minh") || lang.Contains("lồng tiếng") || lang.Contains("long tieng"))
                           && !lang.Contains("vietsub");

        return !isDubbedOnly;
    }

    /// <summary>
    /// Determines if a specific episode server is a Vietsub server.
    /// Rejects Thuyết Minh and Lồng Tiếng servers.
    /// </summary>
    public static bool IsVietsubServer(string? serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName)) return true;

        var lower = serverName.ToLowerInvariant();

        // Reject dubbed servers
        if (lower.Contains("thuyết minh") || lower.Contains("thuyet minh") ||
            lower.Contains("thuyếtminh") || lower.Contains("thuyetminh") ||
            lower.Contains("lồng tiếng") || lower.Contains("long tieng") ||
            lower.Contains("lồngtiếng") || lower.Contains("longtieng"))
        {
            // If it somehow says "Vietsub + Thuyết Minh", allow it
            if (lower.Contains("vietsub"))
            {
                return true;
            }
            return false;
        }

        // If it explicitly says Vietsub, accept
        if (lower.Contains("vietsub"))
        {
            return true;
        }

        // Default servers with no dubbing indication
        return true;
    }
}
