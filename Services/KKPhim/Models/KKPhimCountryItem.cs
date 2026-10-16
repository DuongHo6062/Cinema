using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimCountryItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("_id")]
    public string? AltId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;
}
