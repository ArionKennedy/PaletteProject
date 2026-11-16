using System;
using Backend.Providers.Tmdb;
using Xunit;

namespace Palette.Tests.Unit.Tmdb;

public class TmdbMapperTests
{
    private const string ImageBaseUrl =
        "https://image.tmdb.org/t/p/w500";

    [Fact]
    public void MovieSearchMapsAllFields()
    {
        var source = new TmdbMovieSummary(
            Id: 42,
            Title: "Example Movie",
            ReleaseDate: "2024-03-15",
            PosterPath: "/movie.jpg");

        var result = TmdbMapper.ToSearchResult(source, ImageBaseUrl);

        Assert.Equal("42", result.ExternalId);
        Assert.Equal("movie", result.MediaType);
        Assert.Equal("Example Movie", result.Title);
        Assert.Equal(new DateOnly(2024, 3, 15), result.ReleaseDate);
        Assert.Equal(
            "https://image.tmdb.org/t/p/w500/movie.jpg",
            result.PosterUrl);
    }

    [Fact]
    public void TvSearchMapsNameAndFirstAirDate()
    {
        var source = new TmdbTvSummary(
            Id: 84,
            Name: "Example Series",
            FirstAirDate: "2023-09-20",
            PosterPath: "/series.jpg");

        var result = TmdbMapper.ToSearchResult(source, ImageBaseUrl);

        Assert.Equal("84", result.ExternalId);
        Assert.Equal("tv", result.MediaType);
        Assert.Equal("Example Series", result.Title);
        Assert.Equal(new DateOnly(2023, 9, 20), result.ReleaseDate);
        Assert.Equal(
            "https://image.tmdb.org/t/p/w500/series.jpg",
            result.PosterUrl);
    }

    [Fact]
    public void MovieDetailsMapsAllFields()
    {
        var source = new TmdbMovieDetails(
            Id: 42,
            Title: "Example Movie",
            Overview: "A movie description.",
            ReleaseDate: "2024-03-15",
            PosterPath: "/movie.jpg",
            Runtime: 120);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Equal("42", result.ExternalId);
        Assert.Equal("Example Movie", result.Title);
        Assert.Equal(new DateOnly(2024, 3, 15), result.ReleaseDate);
        Assert.Equal("A movie description.", result.Description);
        Assert.Equal(
            "https://image.tmdb.org/t/p/w500/movie.jpg",
            result.PosterUrl);
        Assert.Equal(120, result.Runtime);
    }

    [Fact]
    public void TvDetailsMapsAllFields()
    {
        var source = new TmdbTvDetails(
            Id: 84,
            Name: "Example Series",
            Overview: "A series description.",
            FirstAirDate: "2023-09-20",
            PosterPath: "/series.jpg",
            NumberOfSeasons: 5);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Equal("84", result.ExternalId);
        Assert.Equal("Example Series", result.Title);
        Assert.Equal(new DateOnly(2023, 9, 20), result.ReleaseDate);
        Assert.Equal("A series description.", result.Description);
        Assert.Equal(
            "https://image.tmdb.org/t/p/w500/series.jpg",
            result.PosterUrl);
        Assert.Equal(5, result.NumberOfSeasons);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void MissingOrNonPositiveMovieRuntimeMapsToNull(int? runtime)
    {
        var source = new TmdbMovieDetails(
            Id: 42,
            Title: "Example Movie",
            Overview: null,
            ReleaseDate: null,
            PosterPath: null,
            Runtime: runtime);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Null(result.Runtime);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(120)]
    public void PositiveMovieRuntimeIsPreserved(int runtime)
    {
        var source = new TmdbMovieDetails(
            Id: 42,
            Title: "Example Movie",
            Overview: null,
            ReleaseDate: null,
            PosterPath: null,
            Runtime: runtime);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Equal(runtime, result.Runtime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void MissingOrNonPositiveSeasonCountMapsToNull(int? seasons)
    {
        var source = new TmdbTvDetails(
            Id: 84,
            Name: "Example Series",
            Overview: null,
            FirstAirDate: null,
            PosterPath: null,
            NumberOfSeasons: seasons);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Null(result.NumberOfSeasons);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void PositiveSeasonCountIsPreserved(int seasons)
    {
        var source = new TmdbTvDetails(
            Id: 84,
            Name: "Example Series",
            Overview: null,
            FirstAirDate: null,
            PosterPath: null,
            NumberOfSeasons: seasons);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Equal(seasons, result.NumberOfSeasons);
    }

    [Theory]
    [InlineData("2024-03-15", 2024, 3, 15)]
    [InlineData("2024-02-29", 2024, 2, 29)]
    [InlineData("2000-01-01", 2000, 1, 1)]
    public void ValidReleaseDateIsParsed(
        string raw,
        int year,
        int month,
        int day)
    {
        var result = TmdbMapper.ParseReleaseDate(raw);

        Assert.Equal(new DateOnly(year, month, day), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    [InlineData("not-a-date")]
    [InlineData("2023-02-29")]
    [InlineData("2024-13-01")]
    [InlineData("2024-04-31")]
    [InlineData("2024-3-5")]
    [InlineData("15/03/2024")]
    public void MissingOrInvalidReleaseDateMapsToNull(string? raw)
    {
        var result = TmdbMapper.ParseReleaseDate(raw);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(
        "/poster.jpg",
        "https://image.tmdb.org/t/p/w500",
        "https://image.tmdb.org/t/p/w500/poster.jpg")]
    [InlineData(
        "poster.jpg",
        "https://image.tmdb.org/t/p/w500/",
        "https://image.tmdb.org/t/p/w500/poster.jpg")]
    [InlineData(
        "/poster.jpg",
        "https://image.tmdb.org/t/p/w500/",
        "https://image.tmdb.org/t/p/w500/poster.jpg")]
    [InlineData(
        "///poster.jpg",
        "https://image.tmdb.org/t/p/w500///",
        "https://image.tmdb.org/t/p/w500/poster.jpg")]
    public void PosterUrlJoinsBaseAndPathWithOneSlash(
        string posterPath,
        string imageBaseUrl,
        string expected)
    {
        var result = TmdbMapper.BuildPosterUrl(
            posterPath,
            imageBaseUrl);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void MissingOrBlankPosterPathMapsToNull(string? posterPath)
    {
        var result = TmdbMapper.BuildPosterUrl(
            posterPath,
            ImageBaseUrl);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void MissingOrBlankMovieOverviewMapsToNull(string? overview)
    {
        var source = new TmdbMovieDetails(
            Id: 42,
            Title: "Example Movie",
            Overview: overview,
            ReleaseDate: null,
            PosterPath: null,
            Runtime: null);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Null(result.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void MissingOrBlankTvOverviewMapsToNull(string? overview)
    {
        var source = new TmdbTvDetails(
            Id: 84,
            Name: "Example Series",
            Overview: overview,
            FirstAirDate: null,
            PosterPath: null,
            NumberOfSeasons: null);

        var result = TmdbMapper.ToLookupResult(source, ImageBaseUrl);

        Assert.Null(result.Description);
    }

    [Fact]
    public void MovieSearchPreservesMissingOptionalFieldsAsNull()
    {
        var source = new TmdbMovieSummary(
            Id: 42,
            Title: "Example Movie",
            ReleaseDate: null,
            PosterPath: null);

        var result = TmdbMapper.ToSearchResult(source, ImageBaseUrl);

        Assert.Null(result.ReleaseDate);
        Assert.Null(result.PosterUrl);
    }

    [Fact]
    public void TvSearchPreservesMissingOptionalFieldsAsNull()
    {
        var source = new TmdbTvSummary(
            Id: 84,
            Name: "Example Series",
            FirstAirDate: null,
            PosterPath: null);

        var result = TmdbMapper.ToSearchResult(source, ImageBaseUrl);

        Assert.Null(result.ReleaseDate);
        Assert.Null(result.PosterUrl);
    }
}