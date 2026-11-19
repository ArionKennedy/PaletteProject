using System;

namespace Palette.Api.MachineLearning.ImplicitFeedback;

public sealed record UserInteraction(
    Guid UserId,
    Guid MediaId,
    InteractionType Type,
    DateTimeOffset OccurredAt
);