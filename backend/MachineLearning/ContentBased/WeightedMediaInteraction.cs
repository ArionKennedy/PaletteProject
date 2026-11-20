namespace Palette.Api.MachineLearning.ContentBased;

public sealed record WeightedMediaInteraction(
    MediaFeatures Media,
    float Weight
);