using Backend.Providers.GoogleBooks;

namespace Palette.Tests.Unit.GoogleBooks;

public class GoogleBooksMapperTests
{
    // ---------- ToSearchResult ----------

    [Fact]
    public void SearchResultMapsAllFields()
    {
        var volume = Volume(
            id: "abc123",
            title: "Dune",
            publishedDate: "1965-08-01",
            thumbnail: "http://img.example/dune.jpg");

        var result = GoogleBooksMapper.ToSearchResult(volume);

        Assert.Equal("abc123", result.ExternalId);
        Assert.Equal("book", result.MediaType);
        Assert.Equal("Dune", result.Title);
        Assert.Equal(new DateOnly(1965, 8, 1), result.ReleaseDate);
        Assert.Equal("https://img.example/dune.jpg", result.PosterUrl);
    }

    [Fact]
    public void SearchResultWithoutImageLinksOrDateHasNullPosterAndDate()
    {
        var volume = new GoogleBooksVolume(
            "abc123",
            new GoogleBooksVolumeInfo("Dune", null, null, ImageLinks: null, null));

        var result = GoogleBooksMapper.ToSearchResult(volume);

        Assert.Null(result.PosterUrl);
        Assert.Null(result.ReleaseDate);
    }

    // ---------- ToLookupResult ----------

    [Fact]
    public void LookupResultMapsAllFields()
    {
        var volume = Volume(
            id: "abc123",
            title: "Dune",
            publishedDate: "1965",
            description: "<p>Spice &amp; sand.</p>",
            thumbnail: "http://img.example/dune.jpg",
            identifiers:
            [
                new("ISBN_10", "0441013597"),
                new("ISBN_13", "9780441013593"),
            ]);

        var result = GoogleBooksMapper.ToLookupResult(volume);

        Assert.Equal("abc123", result.ExternalId);
        Assert.Equal("Dune", result.Title);
        Assert.Equal(new DateOnly(1965, 1, 1), result.ReleaseDate);
        Assert.Equal("Spice & sand.", result.Description);
        Assert.Equal("https://img.example/dune.jpg", result.PosterUrl);
        Assert.Equal("9780441013593", result.ISBN);
    }

    [Fact]
    public void LookupResultWithOnlyRequiredFieldsLeavesOptionalFieldsNull()
    {
        var volume = new GoogleBooksVolume(
            "abc123",
            new GoogleBooksVolumeInfo("Dune", null, null, null, null));

        var result = GoogleBooksMapper.ToLookupResult(volume);

        Assert.Null(result.ReleaseDate);
        Assert.Null(result.Description);
        Assert.Null(result.PosterUrl);
        Assert.Null(result.ISBN);
    }

    // ---------- ParsePublishedDate ----------

    [Theory]
    [InlineData("2024-03-15", 2024, 3, 15)]
    [InlineData("2024-03", 2024, 3, 1)]
    [InlineData("2024", 2024, 1, 1)]
    public void ParsePublishedDateAcceptsFullMonthAndYearFormats(
        string raw, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), GoogleBooksMapper.ParsePublishedDate(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-date")]
    [InlineData("2024-13-45")]
    [InlineData("15/03/2024")]
    public void ParsePublishedDateReturnsNullForMissingOrInvalidValues(string? raw)
    {
        Assert.Null(GoogleBooksMapper.ParsePublishedDate(raw));
    }

    // ---------- BuildPosterUrl ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildPosterUrlReturnsNullForMissingValues(string? url)
    {
        Assert.Null(GoogleBooksMapper.BuildPosterUrl(url));
    }

    [Theory]
    [InlineData("http://img.example/a.jpg", "https://img.example/a.jpg")]
    [InlineData("https://img.example/a.jpg", "https://img.example/a.jpg")]
    public void BuildPosterUrlForcesHttps(string url, string expected)
    {
        Assert.Equal(expected, GoogleBooksMapper.BuildPosterUrl(url));
    }

    // ---------- PickIsbn (through ToLookupResult, since it is private) ----------

    [Fact]
    public void IsbnIsNullWhenIdentifiersAreNull()
    {
        Assert.Null(LookupIsbn(null));
    }

    [Fact]
    public void IsbnIsNullWhenIdentifiersAreEmpty()
    {
        Assert.Null(LookupIsbn([]));
    }

    [Fact]
    public void IsbnPrefersIsbn13OverIsbn10RegardlessOfOrder()
    {
        var isbn = LookupIsbn(
        [
            new("ISBN_10", "0441013597"),
            new("ISBN_13", "9780441013593"),
        ]);

        Assert.Equal("9780441013593", isbn);
    }

    [Fact]
    public void IsbnFallsBackToIsbn10WhenNoIsbn13Exists()
    {
        Assert.Equal("0441013597", LookupIsbn([new("ISBN_10", "0441013597")]));
    }

    [Fact]
    public void IsbnTypeMatchingIsCaseInsensitive()
    {
        Assert.Equal("9780441013593", LookupIsbn([new("isbn_13", "9780441013593")]));
    }

    [Fact]
    public void IsbnSkipsBlankIdentifiersAndFallsBackToTheNextType()
    {
        var isbn = LookupIsbn(
        [
            new("ISBN_13", "   "),
            new("ISBN_10", "0441013597"),
        ]);

        Assert.Equal("0441013597", isbn);
    }

    [Fact]
    public void IsbnIsNullWhenOnlyUnknownTypesArePresent()
    {
        Assert.Null(LookupIsbn([new("OTHER", "OCLC:123")]));
    }

    [Fact]
    public void IsbnIsTrimmed()
    {
        Assert.Equal("9780441013593", LookupIsbn([new("ISBN_13", "  9780441013593  ")]));
    }

    // ---------- CleanDescription ----------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t ")]
    public void CleanDescriptionReturnsNullForMissingValues(string? html)
    {
        Assert.Null(GoogleBooksMapper.CleanDescription(html));
    }

    [Theory]
    [InlineData("<p></p><br/>")]
    [InlineData("<b></b>")]
    [InlineData("&nbsp;")]
    public void CleanDescriptionReturnsNullWhenNothingRemainsAfterCleaning(string html)
    {
        Assert.Null(GoogleBooksMapper.CleanDescription(html));
    }

    [Theory]
    [InlineData("one<br>two", "one\ntwo")]
    [InlineData("one<br/>two", "one\ntwo")]
    [InlineData("one<BR />two", "one\ntwo")]
    [InlineData("<p>one</p><p>two</p>", "one\n\ntwo")]
    [InlineData("<ul><li>one</li><li>two</li></ul>", "one\ntwo")]
    public void CleanDescriptionTurnsStructuralTagsIntoLineBreaks(string html, string expected)
    {
        Assert.Equal(expected, GoogleBooksMapper.CleanDescription(html));
    }

    [Fact]
    public void CleanDescriptionStripsOtherTagsAndKeepsText()
    {
        Assert.Equal("bold and italic", GoogleBooksMapper.CleanDescription("<b>bold</b> and <i>italic</i>"));
    }

    [Fact]
    public void CleanDescriptionDecodesHtmlEntitiesAfterStripping()
    {
        Assert.Equal("Tom & Jerry's <tag>", GoogleBooksMapper.CleanDescription("Tom &amp; Jerry&#39;s &lt;tag&gt;"));
    }

    [Fact]
    public void CleanDescriptionRemovesTrailingSpacesBeforeNewlines()
    {
        Assert.Equal("one\ntwo", GoogleBooksMapper.CleanDescription("one \t<br>two"));
    }

    [Fact]
    public void CleanDescriptionCollapsesThreeOrMoreNewlinesIntoOneBlankLine()
    {
        Assert.Equal("one\n\ntwo", GoogleBooksMapper.CleanDescription("one<br><br><br><br>two"));
    }

    [Fact]
    public void CleanDescriptionTrimsLeadingAndTrailingWhitespace()
    {
        Assert.Equal("text", GoogleBooksMapper.CleanDescription("  <p>text</p>  "));
    }

    // ---------- helpers ----------

    private static string? LookupIsbn(List<GoogleBooksIdentifier>? identifiers) =>
        GoogleBooksMapper.ToLookupResult(Volume(identifiers: identifiers)).ISBN;

    private static GoogleBooksVolume Volume(
        string id = "id-1",
        string title = "Title",
        string? publishedDate = null,
        string? description = null,
        string? thumbnail = null,
        List<GoogleBooksIdentifier>? identifiers = null) =>
        new(
            id,
            new GoogleBooksVolumeInfo(
                title,
                publishedDate,
                description,
                thumbnail is null ? null : new GoogleBooksImageLinks(thumbnail),
                identifiers));
}
