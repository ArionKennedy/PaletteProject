using System.Diagnostics;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Baselines;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Evaluation;
using Palette.Api.MachineLearning.Optimization;

var ratings = DataGenerator.Generate();
var split = DatasetSplitter.Split(ratings);

Console.WriteLine("HYPERPARAMETER OPTIMIZATION");
Console.WriteLine($"Total ratings: {ratings.Count}");
Console.WriteLine($"Training: {split.Training.Count}");
Console.WriteLine($"Validation: {split.Validation.Count}");
Console.WriteLine($"Testing: {split.Testing.Count}");

// Establish baseline
var baseline = new GlobalMeanModel();
baseline.Train(split.Training);

double baselineRmse = ModelEvaluator.CalculateRmse(
    split.Validation,
    baseline.Predict
);

Console.WriteLine($"Baseline RMSE: {baselineRmse:F6}");
Console.WriteLine("\nRunning grid search...");

var stopwatch = Stopwatch.StartNew();

var results = HyperparameterOptimizer.Optimize(split);

stopwatch.Stop();

Console.WriteLine($"\nConfigurations evaluated: {results.Count}");
Console.WriteLine($"Optimization time: {stopwatch.Elapsed.TotalSeconds:F4}s");

Console.WriteLine("\nTOP 10 CONFIGURATIONS");

foreach (var result in results.Take(10))
{
    var p = result.Parameters;

    Console.WriteLine(
        $"Factors: {p.FactorCount,2} | " +
        $"Epochs: {p.Epochs,3} | " +
        $"LR: {p.LearningRate:F3} | " +
        $"Reg: {p.Regularization:F3} | " +
        $"Train RMSE: {result.TrainingRmse:F6} | " +
        $"Val RMSE: {result.ValidationRmse:F6}"
    );
}

var best = results.First();

double improvement =
    (baselineRmse - best.ValidationRmse)
    / baselineRmse * 100;

Console.WriteLine("\nBEST CONFIGURATION");
Console.WriteLine($"Factors: {best.Parameters.FactorCount}");
Console.WriteLine($"Epochs: {best.Parameters.Epochs}");
Console.WriteLine($"Learning rate: {best.Parameters.LearningRate}");
Console.WriteLine($"Regularization: {best.Parameters.Regularization}");
Console.WriteLine($"Training RMSE: {best.TrainingRmse:F6}");
Console.WriteLine($"Validation RMSE: {best.ValidationRmse:F6}");
Console.WriteLine($"Training time: {best.TrainingTimeMs:F4}ms");
Console.WriteLine($"Baseline RMSE: {baselineRmse:F6}");
Console.WriteLine($"Improvement over baseline: {improvement:F2}%");