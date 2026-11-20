namespace Palette.Api.MachineLearning.Data;

// Responsible for storing the results of our splitting algorithm.
public sealed record DatasetSplit(
    IReadOnlyList<RatingObservation> Training,
    IReadOnlyList<RatingObservation> Validation,
    IReadOnlyList<RatingObservation> Testing
);