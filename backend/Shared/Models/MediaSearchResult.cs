namespace Backend.Shared.Models;

public sealed record MediaSearchResult(
    string ExternalId,
    string MediaType,
    string Title,
    DateOnly? ReleaseDate,
    string? PosterUrl);