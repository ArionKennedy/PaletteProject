using System;
using System.Collections.Generic;

namespace Palette.Api.MachineLearning.ContentBased;

public sealed record UserPreferenceProfile(
    Guid UserId,
    IReadOnlyDictionary<string, float> GenrePreferences,
    IReadOnlyDictionary<string, float> AuthorPreferences,
    IReadOnlyDictionary<string, float> DirectorPreferences,
    IReadOnlyDictionary<string, float> StudioPreferences,
    IReadOnlyDictionary<string, float> PublisherPreferences,
    float? PreferredRuntimeMinutes = null,
    float? PreferredPageCount = null,
    float? PreferredNumberOfSeasons = null
);
