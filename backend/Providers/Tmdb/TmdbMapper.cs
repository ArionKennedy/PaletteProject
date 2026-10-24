using System.Globalization;
using Backend.Constants;
using Backend.Shared.Models;

namespace Backend.Providers.Tmdb;

public static class TmdbMapper
{

    public static MediaSearchResult ToSearchResult(TmdbMovieSummary source, string imageBaseUrl) =>
        new(
            ExternalId: source.Id.ToString(CultureInfo.InvariantCulture),
            MediaType: MediaConstants.MEDIA_TYPE_MOVIE,
            Title: source.Title,
            ReleaseDate: ParseReleaseDate(source.ReleaseDate),
            PosterUrl: BuildPosterUrl(source.PosterPath, imageBaseUrl));

    public static MovieLookupResult ToLookupResult(TmdbMovieDetails source, string imageBaseUrl) =>
        new(
            ExternalId: source.Id.ToString(CultureInfo.InvariantCulture),
            Title: source.Title,
            ReleaseDate: ParseReleaseDate(source.ReleaseDate),
            Description: NullIfBlank(source.Overview),
            PosterUrl: BuildPosterUrl(source.PosterPath, imageBaseUrl),
            // TMDB reports 0 for "unknown", and "0 minutes" is not a real runtime.
            Runtime: source.Runtime is > 0 ? source.Runtime : null);

    /// <summary>TMDB sends "" for a missing date, so anything unparseable becomes null.</summary>
    public static DateOnly? ParseReleaseDate(string? raw) =>
        DateOnly.TryParseExact(
            raw,
            TmdbConstants.RELEASE_DATE_FORMAT,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;

    /// <summary>TMDB sends only a path such as "/abc.jpg"; the client needs the full URL.</summary>
    public static string? BuildPosterUrl(string? posterPath, string imageBaseUrl) =>
        string.IsNullOrWhiteSpace(posterPath)
            ? null
            : $"{imageBaseUrl.TrimEnd('/')}/{posterPath.TrimStart('/')}";

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
