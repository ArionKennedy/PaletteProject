using System.Text.Json.Serialization;
namespace Backend.Providers.Igdb;

public sealed record IgdbGame(
    int Id,
    string Name,
    string? Summary,
    [property: JsonPropertyName("first_release_date")] long? FirstReleaseDate,
    IgdbCover? Cover);

public sealed record IgdbCover(
    [property: JsonPropertyName("image_id")] string ImageId);