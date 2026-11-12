using Backend.Constants;
using Backend.Shared.Models;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Backend.Providers.GoogleBooks;

public static class GoogleBooksMapper
{

    private static readonly string[] DateFormats = ["yyyy-MM-dd", "yyyy-MM", "yyyy"];
    private static readonly string[] IsbnTypePriority = ["ISBN_13", "ISBN_10"];
    public static MediaSearchResult ToSearchResult(GoogleBooksVolume v) => new(
    ExternalId: v.Id,
    MediaType: MediaConstants.MEDIA_TYPE_BOOK,
    Title: v.VolumeInfo.Title,
    ReleaseDate: ParsePublishedDate(v.VolumeInfo.PublishedDate),
    PosterUrl: BuildPosterUrl(v.VolumeInfo.ImageLinks?.Thumbnail));

    public static BookLookupResult ToLookupResult(GoogleBooksVolume v) => new(
        ExternalId: v.Id,
        Title: v.VolumeInfo.Title,
        ReleaseDate: ParsePublishedDate(v.VolumeInfo.PublishedDate),
        Description: CleanDescription(v.VolumeInfo.Description),
        PosterUrl: BuildPosterUrl(v.VolumeInfo.ImageLinks?.Thumbnail),
        ISBN: PickIsbn(v.VolumeInfo.IndustryIdentifiers));

    public static DateOnly? ParsePublishedDate(string? raw) =>
        DateOnly.TryParseExact(
            raw,
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;


    public static string? BuildPosterUrl(string? url) =>
        string.IsNullOrWhiteSpace(url) ? null : url.Replace("http://", "https://");

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? PickIsbn(List<GoogleBooksIdentifier>? identifiers)
    {
        if (identifiers is null || identifiers.Count == 0)
        {
            return null;
        }

        foreach (var wantedType in IsbnTypePriority)
        {
            var match = identifiers.FirstOrDefault(i => string.Equals(i.Type, wantedType, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(i.Identifier));

            if (match is not null)
            {
                return match.Identifier.Trim();
            }
        }

        return null;
    }

    public static string? CleanDescription(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        // 1. Turn structural tags into line breaks BEFORE removing tags.
        var text = Regex.Replace(html, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</p\s*>", "\n\n", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"</li\s*>", "\n", RegexOptions.IgnoreCase);

        // 2. Remove every remaining tag.
        text = Regex.Replace(text, @"<[^>]+>", string.Empty);

        // 3. Decode entities AFTER stripping (&amp; -> &, &#39; -> ').
        text = WebUtility.HtmlDecode(text);

        // 4. Tidy: at most one blank line in a row, then trim.
        text = Regex.Replace(text, @"[ \t]+\n", "\n");
        text = Regex.Replace(text, @"\n{3,}", "\n\n");
        text = text.Trim();

        return text.Length == 0 ? null : text;
    }


}

