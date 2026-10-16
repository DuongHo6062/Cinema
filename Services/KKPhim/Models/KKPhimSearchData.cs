using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimSearchData
{
    [JsonPropertyName("items")]
    public List<KKPhimSearchMovieItem> Items { get; set; } = new();

    [JsonPropertyName("params")]
    public KKPhimSearchParams? Params { get; set; }

    [JsonPropertyName("APP_DOMAIN_CDN_IMAGE")]
    public string? AppDomainCdnImage { get; set; }
}
