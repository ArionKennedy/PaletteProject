namespace Backend.Shared.Models;

public sealed record MovieLookupResult(
    string ExternalId,
    string Title,
    DateOnly? ReleaseDate,
    string? Description,
    string? PosterUrl,
    int? Runtime)
    : MediaLookupResult(ExternalId, Title, ReleaseDate, Description, PosterUrl);
