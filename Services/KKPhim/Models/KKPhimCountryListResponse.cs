using System.Text.Json;
using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimCountryListData
{
    [JsonPropertyName("items")]
    public List<KKPhimCountryItem> Items { get; set; } = new();
}

public class KKPhimCountryListResponse
{
    [JsonPropertyName("status")]
    public JsonElement? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public KKPhimCountryListData? Data { get; set; }
}
