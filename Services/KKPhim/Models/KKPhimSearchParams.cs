using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimSearchParams
{
    [JsonPropertyName("keyword")]
    public string? Keyword { get; set; }

    [JsonPropertyName("pagination")]
    public KKPhimPagination? Pagination { get; set; }
}
