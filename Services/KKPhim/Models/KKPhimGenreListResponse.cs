using System.Text.Json;
using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimGenreListData
{
    [JsonPropertyName("items")]
    public List<KKPhimCategoryItem> Items { get; set; } = new();
}

public class KKPhimGenreListResponse
{
    [JsonPropertyName("status")]
    public JsonElement? Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public KKPhimGenreListData? Data { get; set; }
}
