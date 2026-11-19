using System;
using System.Collections.Generic;
using System.Linq;
using Palette.Api.MachineLearning.Baselines;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Optimization;

namespace Palette.Api.MachineLearning.Evaluation;

public static class RobustnessEvaluator
{
    // Runs seeded experiments comparing matrix factorization with baseline models,
    // then prints per-run metrics and aggregate performance statistics.
    public static void Run(
        Hyperparameters parameters,
        int experiments = 10,
        int startingSeed = 100)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            experiments);

        var globalErrors = new List<double>();
        var userErrors = new List<double>();
        var mediaErrors = new List<double>();
        var matrixErrors = new List<double>();
        var improvements = new List<double>();

        int wins = 0;

        Console.WriteLine("MODEL ROBUSTNESS EVALUATION");
        Console.WriteLine($"Experiments: {experiments}");
        Console.WriteLine($"Parameters: {parameters}\n");

        for (int i = 0; i < experiments; i++)
        {
            int seed = startingSeed + i;

            var ratings = DataGenerator.Generate(seed: seed);

            var split = DatasetSplitter.Split(
                ratings,
                seed: seed
            );

            // Global mean baseline
            var globalModel = new GlobalMeanModel();
            globalModel.Train(split.Training);

            double globalRmse = ModelEvaluator.CalculateRmse(
                split.Validation,
                globalModel.Predict
            );

            // User mean baseline
            var userModel = new UserMeanModel();
            userModel.Train(split.Training);

            double userRmse = ModelEvaluator.CalculateRmse(
                split.Validation,
                userModel.Predict
            );

            // Media mean baseline
            var mediaModel = new MediaMeanModel();
            mediaModel.Train(split.Training);

            double mediaRmse = ModelEvaluator.CalculateRmse(
                split.Validation,
                mediaModel.Predict
            );

            // Matrix factorization
            var matrixModel = new MatrixFactorizationModel(
                parameters.FactorCount
            );

            matrixModel.Train(
                split.Training,
                epochs: parameters.Epochs,
                learningRate: parameters.LearningRate,
                regularization: parameters.Regularization
            );

            double matrixRmse = ModelEvaluator.CalculateRmse(
                split.Validation,
                matrixModel.Predict
            );

            double bestBaseline = Math.Min(
                globalRmse,
                Math.Min(userRmse, mediaRmse)
            );

            double improvement = bestBaseline > 0
                ? (bestBaseline - matrixRmse) / bestBaseline * 100
                : double.NaN;

            if (matrixRmse < bestBaseline)
                wins++;

            globalErrors.Add(globalRmse);
            userErrors.Add(userRmse);
            mediaErrors.Add(mediaRmse);
            matrixErrors.Add(matrixRmse);
            improvements.Add(improvement);

            Console.WriteLine(
                $"Seed: {seed} | " +
                $"Global: {globalRmse:F6} | " +
                $"User: {userRmse:F6} | " +
                $"Media: {mediaRmse:F6} | " +
                $"MF: {matrixRmse:F6} | " +
                $"Improvement: {improvement:F2}%"
            );
        }

        Console.WriteLine("\nAGGREGATE RESULTS");

        PrintStatistics("Global Mean", globalErrors);
        PrintStatistics("User Mean", userErrors);
        PrintStatistics("Media Mean", mediaErrors);
        PrintStatistics("Matrix Factorization", matrixErrors);

        Console.WriteLine("\nROBUSTNESS SUMMARY");

        Console.WriteLine(
            $"Wins: {wins}/{experiments}");

        Console.WriteLine(
            $"Win rate: {(double)wins / experiments * 100:F2}%");

        Console.WriteLine(
            $"Average improvement: {improvements.Average():F2}%");
    }

    // Calculates the population standard deviation of the supplied values.
    private static double CalculateStandardDeviation(
        IReadOnlyList<double> values)
    {
        double mean = values.Average();

        double variance = values.Average(
            value => Math.Pow(value - mean, 2)
        );

        return Math.Sqrt(variance);
    }

    // Prints aggregate error statistics for one model.
    private static void PrintStatistics(
        string name,
        IReadOnlyList<double> errors)
    {
        Console.WriteLine($"\n{name}");
        Console.WriteLine($"Mean RMSE: {errors.Average():F6}");
        Console.WriteLine(
            $"Std deviation: {CalculateStandardDeviation(errors):F6}");
        Console.WriteLine($"Minimum RMSE: {errors.Min():F6}");
        Console.WriteLine($"Maximum RMSE: {errors.Max():F6}");
    }
}