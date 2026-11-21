using Backend.Providers.Igdb;
using Backend.Shared.Models;

namespace Palette.Tests.Unit.Igdb;

public class IgdbMapperTests
{
    private const string ImageBase = "https://images.igdb.example/upload/";

    private static IgdbGame Game(
        long? date = 1431993600,
        string? summary = "  A big adventure.  ",
        IgdbCover? cover = null) =>
        new(1942, "The Witcher 3", summary, date, cover ?? new IgdbCover("co1wyy"));

    [Fact]
    public void ToSearchResultMapsEveryField()
    {
        var result = IgdbMapper.ToSearchResult(Game(), ImageBase);

        Assert.Equal("1942", result.ExternalId);
        Assert.Equal("game", result.MediaType);
        Assert.Equal("The Witcher 3", result.Title);
        Assert.Equal(new DateOnly(2015, 5, 19), result.ReleaseDate);
        Assert.Equal(ImageBase + "t_cover_big/co1wyy.jpg", result.PosterUrl);
    }

    [Fact]
    public void ToLookupResultTrimsTheSummaryAndIsAGameResult()
    {
        var result = IgdbMapper.ToLookupResult(Game(), ImageBase);

        Assert.IsType<GameLookupResult>(result);
        Assert.Equal("A big adventure.", result.Description);
    }

    [Fact]
    public void MissingReleaseDateMapsToNull() =>
        Assert.Null(IgdbMapper.ParseReleaseDate(null));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankSummaryMapsToNull(string? summary) =>
        Assert.Null(IgdbMapper.CleanDescription(summary));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void MissingImageIdGivesNoPoster(string? imageId) =>
        Assert.Null(IgdbMapper.BuildPosterUrl(ImageBase, imageId));

    [Fact]
    public void GameWithNoCoverHasNoPoster()
    {
        var game = new IgdbGame(1, "No Cover", null, null, null);

        Assert.Null(IgdbMapper.ToSearchResult(game, ImageBase).PosterUrl);
        Assert.Null(IgdbMapper.ToLookupResult(game, ImageBase).PosterUrl);
    }
}
