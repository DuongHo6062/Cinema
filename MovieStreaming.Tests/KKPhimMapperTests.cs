using MovieStreaming.Models.Entities;
using MovieStreaming.Services.KKPhim;
using MovieStreaming.Services.KKPhim.Models;

namespace MovieStreaming.Tests;

public class KKPhimMapperTests
{
    [Fact]
    public void MapToMovie_CompleteData_MapsAllFieldsCorrectly()
    {
        // Arrange
        var item = new KKPhimMovieDetailItem
        {
            Id = "6a673496849af893f4074aba",
            Name = "Anh Sam Của Em",
            Slug = "anh-sam-cua-em",
            OriginName = "Your Third",
            Content = "<p>Nội dung phim test &amp; demo</p>",
            Type = "series",
            Status = "ongoing",
            Quality = "FHD",
            Lang = "Vietsub",
            Year = 2026,
            Time = "45 phút/tập",
            ThumbUrl = "uploads/movies/thumb.webp",
            PosterUrl = "https://phimimg.com/uploads/movies/poster.webp",
            Tmdb = new KKPhimTmdbItem { VoteAverage = 8.5m },
            Country = new List<KKPhimCountryItem>
            {
                new() { Name = "Thái Lan", Slug = "thai-lan" },
                new() { Name = "Hàn Quốc", Slug = "han-quoc" }
            }
        };

        // Act
        var result = KKPhimMapper.MapToMovie(item);

        // Assert
        Assert.Equal("Anh Sam Của Em", result.Title);
        Assert.Equal("anh-sam-cua-em", result.Slug);
        Assert.Equal("Your Third", result.OriginalTitle);
        Assert.Equal("Nội dung phim test & demo", result.Description);
        Assert.Equal("series", result.MovieType);
        Assert.Equal("ongoing", result.Status);
        Assert.Equal("FHD", result.Quality);
        Assert.Equal("Vietsub", result.Language);
        Assert.Equal(2026, result.ReleaseYear);
        Assert.Equal(45, result.Duration);
        Assert.Equal("https://phimimg.com/uploads/movies/thumb.webp", result.ThumbnailUrl);
        Assert.Equal("https://phimimg.com/uploads/movies/poster.webp", result.PosterUrl);
        Assert.Equal(8.5m, result.Rating);
        Assert.Equal("Thái Lan, Hàn Quốc", result.Country);
        Assert.Equal("6a673496849af893f4074aba", result.ExternalId);
        Assert.Equal("KKPHIM", result.ExternalSource);
    }

    [Fact]
    public void MapToMovie_MissingOptionalFields_HandlesSafely()
    {
        // Arrange
        var item = new KKPhimMovieDetailItem
        {
            Id = "test-id",
            Name = "Minimal Movie",
            Slug = "minimal-movie"
        };

        // Act
        var result = KKPhimMapper.MapToMovie(item);

        // Assert
        Assert.Equal("Minimal Movie", result.Title);
        Assert.Equal("minimal-movie", result.Slug);
        Assert.Null(result.OriginalTitle);
        Assert.Null(result.Description);
        Assert.Null(result.ThumbnailUrl);
        Assert.Null(result.PosterUrl);
        Assert.Null(result.Rating);
        Assert.Null(result.Duration);
        Assert.Null(result.Country);
    }

    [Theory]
    [InlineData("Tập 01", 1)]
    [InlineData("Tập 12", 12)]
    [InlineData("Full", 1)]
    [InlineData("", 1)]
    [InlineData(null, 1)]
    [InlineData("Tập 105", 105)]
    public void ParseEpisodeNumber_VariousInputs_ReturnsExpected(string? input, int expected)
    {
        var result = KKPhimMapper.ParseEpisodeNumber(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("131 phút", 131)]
    [InlineData("45 phút/tập", 45)]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void ParseDurationMinutes_VariousInputs_ReturnsExpected(string? input, int? expected)
    {
        var result = KKPhimMapper.ParseDurationMinutes(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void StripHtml_WithTagsAndEntities_ReturnsCleanText()
    {
        var html = "<div><p>Phim hay &amp; đặc sắc</p></div>";
        var result = KKPhimMapper.StripHtml(html);
        Assert.Equal("Phim hay & đặc sắc", result);
    }
}
