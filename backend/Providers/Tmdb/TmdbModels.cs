
using System.Text.Json.Serialization;

namespace Backend.Providers.Tmdb;

public sealed record TmdbSearchResponse<T>(
    int Page,
    [property: JsonRequired] List<T> Results,
    int TotalPages,
    int TotalResults);

public sealed record TmdbMovieSummary(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Title,
    string? ReleaseDate,
    string? PosterPath,
    int VoteCount = 0,
    float? VoteAverage = null);

public sealed record TmdbGenre(
    int Id,
    string Name);

public sealed record TmdbGenreListResponse(
    [property: JsonRequired] List<TmdbGenre> Genres);

public sealed record TmdbMovieDetails(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Title,
    string? Overview,
    string? ReleaseDate,
    string? PosterPath,
    int? Runtime,
    List<TmdbGenre>? Genres,
    int VoteCount = 0,
    float? VoteAverage = null);

public sealed record TmdbTvSummary(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Name,
    string? FirstAirDate,
    string? PosterPath,
    int VoteCount = 0,
    float? VoteAverage = null);

public sealed record TmdbTvDetails(
    [property: JsonRequired] int Id,
    [property: JsonRequired] string Name,
    string? Overview,
    string? FirstAirDate,
    string? PosterPath,
    int? NumberOfSeasons,
    int VoteCount = 0,
    float? VoteAverage = null);

    