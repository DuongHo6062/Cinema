namespace MovieStreaming.Services;

public static class SourceTypeResolver
{
    public static string Resolve(string? storedType, string? videoUrl)
    {
        if (!string.IsNullOrWhiteSpace(storedType))
        {
            var trimmed = storedType.Trim();
            if (trimmed.Equals("m3u8", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("hls", StringComparison.OrdinalIgnoreCase))
            {
                return "HLS";
            }

            if (trimmed.Equals("embed", StringComparison.OrdinalIgnoreCase))
            {
                return "EMBED";
            }

            if (trimmed.Equals("mp4", StringComparison.OrdinalIgnoreCase))
            {
                return "MP4";
            }

            return trimmed.ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(videoUrl))
        {
            if (videoUrl.Contains(".m3u8", StringComparison.OrdinalIgnoreCase))
            {
                return "HLS";
            }

            if (videoUrl.Contains(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                return "MP4";
            }

            if (videoUrl.Contains("embed", StringComparison.OrdinalIgnoreCase) ||
                videoUrl.Contains("player", StringComparison.OrdinalIgnoreCase))
            {
                return "EMBED";
            }
        }

        return "UNKNOWN";
    }
}
