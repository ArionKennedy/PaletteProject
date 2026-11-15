namespace Backend.Shared.Models;

public sealed record TvLookupResult(
    string ExternalId,
    string Title,
    DateOnly? ReleaseDate,
    string? Description,
    string? PosterUrl,
    int? NumberOfSeasons)
    : MediaLookupResult(ExternalId, Title, ReleaseDate, Description, PosterUrl);
