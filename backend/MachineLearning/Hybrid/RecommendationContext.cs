using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;

namespace Palette.Api.MachineLearning.Hybrid;

public sealed record RecommendationContext(
    UserPreferenceProfile Profile,
    IReadOnlySet<Guid> ExcludedMediaIds,
    int MaxReviewCount,
    float GlobalAverageRating = 3f,
    float BayesianPriorWeight = 50f
);
