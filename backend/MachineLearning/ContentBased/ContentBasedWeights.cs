namespace Palette.Api.MachineLearning.ContentBased;

public sealed record ContentBasedWeights(
    float Genre = 0.40f,
    float Author = 0.15f,
    float Director = 0.15f,
    float Studio = 0.10f,
    float Publisher = 0.05f,
    float Length = 0.15f,
    float RuntimeTolerance = 45f,
    float PageCountTolerance = 100f,
    float SeasonCountTolerance = 2f
);