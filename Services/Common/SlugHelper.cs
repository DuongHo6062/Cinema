using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MovieStreaming.Services.Common;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        // Normalize and remove diacritics
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        var cleanText = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

        // Replace invalid chars with hyphen
        cleanText = Regex.Replace(cleanText, @"[^a-z0-9\s-]", "");
        // Convert multiple spaces/hyphens into single hyphen
        cleanText = Regex.Replace(cleanText, @"[\s-]+", "-").Trim('-');

        return cleanText;
    }
}
