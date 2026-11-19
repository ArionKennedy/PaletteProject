using System;
using System.Collections.Generic;

namespace Palette.Api.MachineLearning.ContentBased;

public sealed record MediaFeatures(
    Guid MediaId,
    string MediaType,
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> Authors,
    IReadOnlyList<string> Directors,
    IReadOnlyList<string> Studios,
    IReadOnlyList<string> Publishers,
    int? RuntimeMinutes = null,
    int? PageCount = null,
    int? NumberOfSeasons = null
);