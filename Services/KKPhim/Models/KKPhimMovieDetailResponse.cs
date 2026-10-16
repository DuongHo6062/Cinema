using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimMovieDetailResponse
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("movie")]
    public KKPhimMovieDetailItem? Movie { get; set; }

    [JsonPropertyName("episodes")]
    public List<KKPhimServerItem>? Episodes { get; set; }
}
