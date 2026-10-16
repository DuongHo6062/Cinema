using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimServerDataItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("filename")]
    public string? Filename { get; set; }

    [JsonPropertyName("link_embed")]
    public string? LinkEmbed { get; set; }

    [JsonPropertyName("link_m3u8")]
    public string? LinkM3u8 { get; set; }
}
