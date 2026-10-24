using System.Text.Json.Serialization;

namespace Backend.Providers.Tmdb;

public sealed record TmdbSearchResponse(
    int Page,
    [property: JsonRequired] List<TmdbMovieSummary> Results,
    int TotalPages,
    int TotalResults);

public sealed record TmdbMovieSummary(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Title,
    string? ReleaseDate,
    string? PosterPath);

public sealed record TmdbMovieDetails(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Title,
    string? Overview,
    string? ReleaseDate,
    string? PosterPath,
    int? Runtime);
