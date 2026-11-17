using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace Backend.Providers.GoogleBooks;

public sealed record GoogleBooksSearchResponse(
    List<GoogleBooksVolume>? Items
);

public sealed record GoogleBooksVolume(
    [property: JsonRequired] string Id,
    [property: JsonRequired] GoogleBooksVolumeInfo VolumeInfo
);

public sealed record GoogleBooksVolumeInfo(
    [property: JsonRequired] string Title,
    string? PublishedDate,
    string? Description,
    GoogleBooksImageLinks? ImageLinks,
    List<GoogleBooksIdentifier>? IndustryIdentifiers
);

public sealed record GoogleBooksImageLinks(string? Thumbnail);
public sealed record GoogleBooksIdentifier(string Type, string Identifier);