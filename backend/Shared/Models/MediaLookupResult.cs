namespace Backend.Shared.Models;

public abstract record MediaLookupResult(
    string ExternalId,
    string Title,
    DateOnly? ReleaseDate,
    string? Description,
    string? PosterUrl);
