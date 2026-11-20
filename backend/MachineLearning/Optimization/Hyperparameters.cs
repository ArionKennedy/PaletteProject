namespace Palette.Api.MachineLearning.Optimization;

public sealed record Hyperparameters(
    int FactorCount,
    int Epochs,
    float LearningRate,
    float Regularization
);