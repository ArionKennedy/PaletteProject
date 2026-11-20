using Backend.Constants;
using Backend.Shared.Models;

namespace Backend.Providers.Igdb;

public static class IgdbMapper
{
    public static MediaSearchResult ToSearchResult(IgdbGame g, string imageBaseUrl) => new(
        ExternalId: g.Id.ToString(),
        MediaType: MediaConstants.MEDIA_TYPE_GAME,
        Title: g.Name,
        ReleaseDate: ParseReleaseDate(g.FirstReleaseDate),
        PosterUrl: BuildPosterUrl(imageBaseUrl, g.Cover?.ImageId));

    public static GameLookupResult ToLookupResult(IgdbGame g, string imageBaseUrl) => new(
        ExternalId: g.Id.ToString(),
        Title: g.Name,
        ReleaseDate: ParseReleaseDate(g.FirstReleaseDate),
        Description: CleanDescription(g.Summary),
        PosterUrl: BuildPosterUrl(imageBaseUrl, g.Cover?.ImageId));

    // IGDB sends Unix seconds (UTC). Missing for unreleased or unannounced games.
    public static DateOnly? ParseReleaseDate(long? unixSeconds) =>
        unixSeconds is null
            ? null
            : DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(unixSeconds.Value).UtcDateTime);

    // IGDB only gives an image_id; the full URL is base + size + id + extension.
    public static string? BuildPosterUrl(string imageBaseUrl, string? imageId) =>
        string.IsNullOrWhiteSpace(imageId)
            ? null
            : $"{imageBaseUrl}{IgdbConstants.COVER_IMAGE_SIZE}/{imageId}.{IgdbConstants.COVER_IMAGE_EXTENSION}";

    public static string? CleanDescription(string? summary) =>
        string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
}
