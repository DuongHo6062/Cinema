using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimServerItem
{
    [JsonPropertyName("server_name")]
    public string ServerName { get; set; } = string.Empty;

    [JsonPropertyName("server_data")]
    public List<KKPhimServerDataItem> ServerData { get; set; } = new();
}
