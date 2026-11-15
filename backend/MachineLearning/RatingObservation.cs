namespace Palette.Api.MachineLearning;

public sealed record RatingObservation(
    Guid UserId,
    Guid MediaId,
    float Rating
);