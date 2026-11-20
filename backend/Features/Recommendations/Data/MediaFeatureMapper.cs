
using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.Enums;
using Palette.Api.Models;
using Palette.Api.MachineLearning.ContentBased;

namespace Backend.Features.Recommendations.Data;

public static class MediaFeatureMapper
{
    // Converts a database media entity into recommendation features.
    public static MediaFeatures Map(
        Media media,
        IReadOnlyList<string>? genres = null,
        IReadOnlyList<string>? authors = null,
        IReadOnlyList<string>? directors = null,
        IReadOnlyList<string>? studios = null,
        IReadOnlyList<string>? publishers = null,
        int? pageCount = null)
    {
        ArgumentNullException.ThrowIfNull(media);

        string mediaType = GetMediaType(media.MediaType);

        return new MediaFeatures(
            MediaId: media.MediaId,
            MediaType: mediaType,
            Genres: NormalizeFeatures(genres),
            Authors: NormalizeFeatures(authors),
            Directors: NormalizeFeatures(directors),
            Studios: NormalizeFeatures(studios),
            Publishers: NormalizeFeatures(publishers),
            RuntimeMinutes: NormalizeLength(media.Movie?.RuntimeMinutes),
            PageCount: NormalizeLength(pageCount),
            NumberOfSeasons: NormalizeLength(media.TvShow?.NumberOfSeasons)
        );
    }

    // Converts the database media type into a supported recommendation type.
    private static string GetMediaType(MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Movie => "movie",
            MediaType.TVShow => "tv",
            MediaType.Book => "book",
            MediaType.Game => "game",
            _ => throw new ArgumentOutOfRangeException(nameof(mediaType))
        };
    }

    // Removes empty, duplicate and unnecessary whitespace from features.
    private static IReadOnlyList<string> NormalizeFeatures(
        IReadOnlyList<string>? features)
    {
        return features?
            .Where(feature => !string.IsNullOrWhiteSpace(feature))
            .Select(feature => feature.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    // Treats missing or invalid media lengths as unavailable.
    private static int? NormalizeLength(int? length)
    {
        return length > 0 ? length : null;
    }
}