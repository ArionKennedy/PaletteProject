namespace Backend.Shared.Models;

public sealed record GameLookupResult(
    string ExternalId,
    string Title,
    DateOnly? ReleaseDate,
    string? Description,
    string? PosterUrl)
    : MediaLookupResult(ExternalId, Title, ReleaseDate, Description, PosterUrl);
