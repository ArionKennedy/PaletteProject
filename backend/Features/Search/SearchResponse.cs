using Backend.Shared.Models;

namespace Backend.Features.Search;

public sealed record SearchResponse(IReadOnlyList<MediaSearchResult> Results);
