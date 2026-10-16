using System.Text.Json.Serialization;

namespace MovieStreaming.Services.KKPhim.Models;

public class KKPhimMovieDetailItem
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = string.Empty;

    [JsonPropertyName("origin_name")]
    public string? OriginName { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("thumb_url")]
    public string? ThumbUrl { get; set; }

    [JsonPropertyName("poster_url")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("episode_current")]
    public string? EpisodeCurrent { get; set; }

    [JsonPropertyName("episode_total")]
    public object? EpisodeTotal { get; set; }

    [JsonPropertyName("quality")]
    public string? Quality { get; set; }

    [JsonPropertyName("lang")]
    public string? Lang { get; set; }

    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("view")]
    public int? View { get; set; }

    [JsonPropertyName("tmdb")]
    public KKPhimTmdbItem? Tmdb { get; set; }

    [JsonPropertyName("category")]
    public List<KKPhimCategoryItem>? Category { get; set; }

    [JsonPropertyName("country")]
    public List<KKPhimCountryItem>? Country { get; set; }
}
