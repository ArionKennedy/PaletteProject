using System;
using System.Collections.Generic;
using System.Diagnostics;
using Palette.Api.MachineLearning.Baselines;
using Palette.Api.MachineLearning.Data;

namespace Palette.Api.MachineLearning.Evaluation;

public static class ModelComparison
{
    public static void Run()
    {
        var ratings = DataGenerator.Generate();

        var split = DatasetSplitter.Split(ratings);

        Console.WriteLine("MODEL PERFORMANCE COMPARISON");
        Console.WriteLine($"Total ratings: {ratings.Count}");
        Console.WriteLine($"Training: {split.Training.Count}");
        Console.WriteLine($"Validation: {split.Validation.Count}");
        Console.WriteLine($"Testing: {split.Testing.Count}");

        var globalModel = new GlobalMeanModel();
        var userModel = new UserMeanModel();
        var mediaModel = new MediaMeanModel();
        var matrixModel = new MatrixFactorizationModel(8);

        EvaluateModel(
            "Global Mean",
            () => globalModel.Train(split.Training),
            globalModel.Predict,
            split);

        EvaluateModel(
            "User Mean",
            () => userModel.Train(split.Training),
            userModel.Predict,
            split);

        EvaluateModel(
            "Media Mean",
            () => mediaModel.Train(split.Training),
            mediaModel.Predict,
            split);

        EvaluateModel(
            "Matrix Factorization",
            () => matrixModel.Train(
                split.Training,
                epochs: 100,
                learningRate: 0.01f,
                regularization: 0.02f),
            matrixModel.Predict,
            split);
    }

    private static void EvaluateModel(
        string name,
        Action train,
        Func<Guid, Guid, float> predict,
        DatasetSplit split)
    {
        var stopwatch = Stopwatch.StartNew();

        train();

        stopwatch.Stop();

        double trainingTime =
            stopwatch.Elapsed.TotalMilliseconds;

        double trainingRmse =
            ModelEvaluator.CalculateRmse(
                split.Training, predict);

        stopwatch.Restart();

        double validationRmse =
            ModelEvaluator.CalculateRmse(
                split.Validation, predict);

        stopwatch.Stop();

        double evaluationTime =
            stopwatch.Elapsed.TotalMilliseconds;

        Console.WriteLine($"\n{name}");
        Console.WriteLine($"Training RMSE: {trainingRmse:F6}");
        Console.WriteLine($"Validation RMSE: {validationRmse:F6}");
        Console.WriteLine($"Training time: {trainingTime:F4} ms");
        Console.WriteLine($"Evaluation time: {evaluationTime:F4} ms");
    }
}