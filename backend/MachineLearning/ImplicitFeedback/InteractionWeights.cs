namespace Palette.Api.MachineLearning.ImplicitFeedback;

public sealed record InteractionWeights(
    float Search = 0.5f,
    float SearchSelection = 1f,
    float Stored = 2f,
    float Completed = 3f,
    float PositiveReview = 5f
);