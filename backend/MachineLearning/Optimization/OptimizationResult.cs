namespace Palette.Api.MachineLearning.Optimization;

public sealed record OptimizationResult(
    Hyperparameters Parameters,
    double TrainingRmse,
    double ValidationRmse,
    double TrainingTimeMs
);