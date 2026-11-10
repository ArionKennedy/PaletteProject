namespace Backend.Shared.Models;

public sealed record BookLookupResult(
    string ExternalId,
    string Title,
    DateOnly? ReleaseDate,
    string? Description,
    string? PosterUrl,
    string? ISBN)
    : MediaLookupResult(ExternalId, Title, ReleaseDate, Description, PosterUrl);
