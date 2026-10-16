namespace MovieStreaming.DTOs;

public class KKPhimSyncRequestDto
{
    /// <summary>
    /// Starting page on KKPhim catalog (1-indexed).
    /// </summary>
    public int FromPage { get; set; } = 1;

    /// <summary>
    /// Ending page on KKPhim catalog.
    /// </summary>
    public int ToPage { get; set; } = 3;

    /// <summary>
    /// Catalog source to query: "au-my" (Western/US/UK), "phim-moi-cap-nhat" (Recently updated), "phim-le" (Single movies), "phim-bo" (Series).
    /// Defaults to "au-my".
    /// </summary>
    public string SourceType { get; set; } = "au-my";

    /// <summary>
    /// When true, only imports movies originating from English-language territories (US, UK, Canada, Australia, etc.).
    /// </summary>
    public bool EnglishOnly { get; set; } = true;

    /// <summary>
    /// When true, only imports movies with Vietnamese subtitles (Vietsub) and rejects dubbed-only versions (Thuyết Minh / Lồng Tiếng).
    /// If both Vietsub and Thuyết Minh are present, only the Vietsub servers will be imported.
    /// </summary>
    public bool VietsubOnly { get; set; } = true;
}
