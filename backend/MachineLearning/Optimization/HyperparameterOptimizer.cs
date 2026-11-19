using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Evaluation;

namespace Palette.Api.MachineLearning.Optimization;

public static class HyperparameterOptimizer
{
    public static IReadOnlyList<OptimizationResult> Optimize(
        DatasetSplit split)
    {
        ArgumentNullException.ThrowIfNull(split);

        var configurations = GenerateConfigurations();

        var results = new List<OptimizationResult>();

        foreach (var parameters in configurations)
        {
            var result = EvaluateConfiguration(
                parameters, split);

            results.Add(result);
        }

        return results
            .OrderBy(result => result.ValidationRmse)
            .ToList();
    }

    private static IEnumerable<Hyperparameters>
        GenerateConfigurations()
    {
        int[] factorCounts = { 1, 2, 3, 4, 8, 16 };

        int[] epochCounts = { 25, 50, 100, 200 };

        float[] learningRates =
        {
            0.001f, 0.005f, 0.01f, 0.02f
        };

        float[] regularizations =
        {
            0f, 0.01f, 0.02f, 0.05f, 0.1f
        };

        foreach (int factors in factorCounts)
        {
            foreach (int epochs in epochCounts)
            {
                foreach (float learningRate in learningRates)
                {
                    foreach (float regularization in regularizations)
                    {
                        yield return new Hyperparameters(
                            factors,
                            epochs,
                            learningRate,
                            regularization
                        );
                    }
                }
            }
        }
    }

    private static OptimizationResult EvaluateConfiguration(
        Hyperparameters parameters,
        DatasetSplit split)
    {
        var model = new MatrixFactorizationModel(
            parameters.FactorCount);

        var stopwatch = Stopwatch.StartNew();

        model.Train(
            split.Training,
            epochs: parameters.Epochs,
            learningRate: parameters.LearningRate,
            regularization: parameters.Regularization
        );

        stopwatch.Stop();

        double trainingTime =
            stopwatch.Elapsed.TotalMilliseconds;

        double trainingRmse = ModelEvaluator.CalculateRmse(
            split.Training,
            model.Predict
        );

        double validationRmse = ModelEvaluator.CalculateRmse(
            split.Validation,
            model.Predict
        );

        return new OptimizationResult(
            parameters,
            trainingRmse,
            validationRmse,
            trainingTime
        );
    }
}