using System.Text.Json;
using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimSearchResponse
{
    [JsonPropertyName("status")]
    public JsonElement? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("data")]
    public KKPhimSearchData? Data { get; set; }
}
