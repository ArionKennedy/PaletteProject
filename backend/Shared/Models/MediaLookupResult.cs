using System.Text.Json.Serialization;

namespace Backend.Shared.Models;

public abstract record MediaLookupResult(
    string ExternalId,
    string Title,
    DateOnly? ReleaseDate,
    string? Description,
    string? PosterUrl)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Saved { get; init; }
}
