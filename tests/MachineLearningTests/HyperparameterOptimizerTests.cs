
using System;
using System.Linq;
using System.Collections.Generic;
using Palette.Api.MachineLearning;
using Palette.Api.MachineLearning.Data;
using Palette.Api.MachineLearning.Evaluation;
using Palette.Api.MachineLearning.Optimization;
using Xunit;

namespace Palette.Tests.MachineLearningTests;

public sealed class HyperparameterOptimizerFixture
{
    public DatasetSplit Split { get; }
    public IReadOnlyList<OptimizationResult> Results { get; }

    public RatingObservation[] OriginalTraining { get; }
    public RatingObservation[] OriginalValidation { get; }
    public RatingObservation[] OriginalTesting { get; }

    // Initializes the shared dataset, snapshots and optimization results.
    public HyperparameterOptimizerFixture()
    {
        Split = DatasetSplitter.Split(
            DataGenerator.Generate(10, 20, 10, seed: 42));

        OriginalTraining = Split.Training.ToArray();
        OriginalValidation = Split.Validation.ToArray();
        OriginalTesting = Split.Testing.ToArray();

        Results = HyperparameterOptimizer.Optimize(Split);
    }
}

public sealed class HyperparameterOptimizerTests
    : IClassFixture<HyperparameterOptimizerFixture>
{
    private readonly HyperparameterOptimizerFixture _fixture;

    private IReadOnlyList<OptimizationResult> Results => _fixture.Results;

    // Initializes the tests using the shared optimization fixture.
    public HyperparameterOptimizerTests(HyperparameterOptimizerFixture fixture)
    {
        _fixture = fixture;
    }

    // Extracts distinct, sorted hyperparameter values.
    private T[] GetParameterValues<T>(Func<Hyperparameters, T> selector)
    {
        return Results.Select(r => selector(r.Parameters))
            .Distinct().OrderBy(v => v).ToArray();
    }

    // Trains an independent model using the supplied hyperparameters.
    private MatrixFactorizationModel TrainModel(Hyperparameters parameters)
    {
        var model = new MatrixFactorizationModel(parameters.FactorCount);

        model.Train(_fixture.Split.Training,
            epochs: parameters.Epochs,
            learningRate: parameters.LearningRate,
            regularization: parameters.Regularization);

        return model;
    }

    // Verifies that repeated optimization produces identical parameters and RMSE.
    private void AssertEquivalentResults(IReadOnlyList<OptimizationResult> repeated)
    {
        Assert.Equal(Results.Count, repeated.Count);

        for (int i = 0; i < Results.Count; i++)
        {
            Assert.Equal(Results[i].Parameters, repeated[i].Parameters);
            Assert.Equal(Results[i].TrainingRmse, repeated[i].TrainingRmse, 6);
            Assert.Equal(Results[i].ValidationRmse, repeated[i].ValidationRmse, 6);
        }
    }

    // Configuration generation

    // Verifies that optimization evaluates all 480 configurations.
    [Fact]
    public void OptimizeReturnsAll480Configurations()
    {
        Assert.Equal(480, Results.Count);
    }

    // Verifies that all evaluated configurations are unique.
    [Fact]
    public void OptimizeReturnsUniqueConfigurations()
    {
        Assert.Equal(Results.Count,
            Results.Select(r => r.Parameters).Distinct().Count());
    }

    // Verifies that all expected factor counts are evaluated.
    [Fact]
    public void OptimizeEvaluatesAllFactorCounts()
    {
        Assert.Equal(new[] { 1, 2, 3, 4, 8, 16 },
            GetParameterValues(p => p.FactorCount));
    }

    // Verifies that all expected epoch counts are evaluated.
    [Fact]
    public void OptimizeEvaluatesAllEpochCounts()
    {
        Assert.Equal(new[] { 25, 50, 100, 200 },
            GetParameterValues(p => p.Epochs));
    }

    // Verifies that all expected learning rates are evaluated.
    [Fact]
    public void OptimizeEvaluatesAllLearningRates()
    {
        Assert.Equal(new[] { 0.001f, 0.005f, 0.01f, 0.02f },
            GetParameterValues(p => p.LearningRate));
    }

    // Verifies that all expected regularization values are evaluated.
    [Fact]
    public void OptimizeEvaluatesAllRegularizations()
    {
        Assert.Equal(new[] { 0f, 0.01f, 0.02f, 0.05f, 0.1f },
            GetParameterValues(p => p.Regularization));
    }

    // Verifies that the original configuration is evaluated.
    [Fact]
    public void OptimizeIncludesOriginalConfiguration()
    {
        var original = new Hyperparameters(8, 100, 0.01f, 0.02f);

        Assert.Contains(Results, r => r.Parameters == original);
    }

    // Verifies that the previously optimal configuration is evaluated.
    [Fact]
    public void OptimizeIncludesPreviouslyOptimalConfiguration()
    {
        var optimized = new Hyperparameters(2, 100, 0.02f, 0.02f);

        Assert.Contains(Results, r => r.Parameters == optimized);
    }

    // Result validation

    // Verifies that all training RMSE values are nonnegative.
    [Fact]
    public void OptimizeReturnsNonNegativeTrainingRmse()
    {
        Assert.All(Results, r => Assert.True(r.TrainingRmse >= 0));
    }

    // Verifies that all validation RMSE values are nonnegative.
    [Fact]
    public void OptimizeReturnsNonNegativeValidationRmse()
    {
        Assert.All(Results, r => Assert.True(r.ValidationRmse >= 0));
    }

    // Verifies that all training RMSE values are finite.
    [Fact]
    public void OptimizeReturnsFiniteTrainingRmse()
    {
        Assert.All(Results, r => Assert.True(double.IsFinite(r.TrainingRmse)));
    }

    // Verifies that all validation RMSE values are finite.
    [Fact]
    public void OptimizeReturnsFiniteValidationRmse()
    {
        Assert.All(Results, r => Assert.True(double.IsFinite(r.ValidationRmse)));
    }

    // Verifies that all training times are nonnegative.
    [Fact]
    public void OptimizeReturnsNonNegativeTrainingTime()
    {
        Assert.All(Results, r => Assert.True(r.TrainingTimeMs >= 0));
    }

    // Verifies that all training times are finite.
    [Fact]
    public void OptimizeReturnsFiniteTrainingTime()
    {
        Assert.All(Results, r => Assert.True(double.IsFinite(r.TrainingTimeMs)));
    }

    // Model evaluation

    // Verifies that reported training RMSE matches independent evaluation.
    [Fact]
    public void OptimizeCalculatesCorrectTrainingRmse()
    {
        var result = Results.First();
        var model = TrainModel(result.Parameters);

        double expected = ModelEvaluator.CalculateRmse(
            _fixture.Split.Training, model.Predict);

        Assert.Equal(expected, result.TrainingRmse, 6);
    }

    // Verifies that reported validation RMSE matches independent evaluation.
    [Fact]
    public void OptimizeCalculatesCorrectValidationRmse()
    {
        var result = Results.First();
        var model = TrainModel(result.Parameters);

        double expected = ModelEvaluator.CalculateRmse(
            _fixture.Split.Validation, model.Predict);

        Assert.Equal(expected, result.ValidationRmse, 6);
    }

    // Verifies that different configurations train independent models.
    [Fact]
    public void OptimizeTrainsIndependentModels()
    {
        var configurations = new[]
        {
            Results.First(),
            Results[Results.Count / 2],
            Results.Last()
        };

        foreach (var result in configurations)
        {
            var model = TrainModel(result.Parameters);

            double expected = ModelEvaluator.CalculateRmse(
                _fixture.Split.Validation, model.Predict);

            Assert.Equal(expected, result.ValidationRmse, 6);
        }
    }

    // Sorting

    // Verifies that results are sorted by ascending validation RMSE.
    [Fact]
    public void OptimizeSortsByAscendingValidationRmse()
    {
        for (int i = 1; i < Results.Count; i++)
            Assert.True(Results[i - 1].ValidationRmse <= Results[i].ValidationRmse);
    }

    // Verifies that the configuration with minimum validation RMSE appears first.
    [Fact]
    public void OptimizeReturnsBestConfigurationFirst()
    {
        Assert.Equal(Results.Min(r => r.ValidationRmse),
            Results.First().ValidationRmse);
    }

    // Reproducibility

    // Verifies that repeated optimization selects the same best configuration.
    [Fact]
    public void OptimizeProducesConsistentBestConfiguration()
    {
        var repeated = HyperparameterOptimizer.Optimize(_fixture.Split);

        Assert.Equal(Results.First().Parameters, repeated.First().Parameters);
    }

    // Verifies that repeated optimization produces consistent RMSE values.
    [Fact]
    public void OptimizeProducesConsistentRmseValues()
    {
        AssertEquivalentResults(
            HyperparameterOptimizer.Optimize(_fixture.Split));
    }

    // Data integrity

    // Verifies that optimization does not modify the training dataset.
    [Fact]
    public void OptimizeDoesNotModifyTrainingDataset()
    {
        Assert.Equal(_fixture.OriginalTraining, _fixture.Split.Training.ToArray());
    }

    // Verifies that optimization does not modify the validation dataset.
    [Fact]
    public void OptimizeDoesNotModifyValidationDataset()
    {
        Assert.Equal(_fixture.OriginalValidation, _fixture.Split.Validation.ToArray());
    }

    // Verifies that optimization does not modify the testing dataset.
    [Fact]
    public void OptimizeDoesNotModifyTestingDataset()
    {
        Assert.Equal(_fixture.OriginalTesting, _fixture.Split.Testing.ToArray());
    }

    // Verifies that optimization does not depend on the testing dataset.
    [Fact]
    public void OptimizeDoesNotDependOnTestingDataset()
    {
        var modifiedSplit = new DatasetSplit(
            _fixture.Split.Training,
            _fixture.Split.Validation,
            []);

        AssertEquivalentResults(
            HyperparameterOptimizer.Optimize(modifiedSplit));
    }

    // Input validation

    // Verifies that null dataset splits are rejected.
    [Fact]
    public void OptimizeRejectsNullDatasetSplit()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HyperparameterOptimizer.Optimize(null!));
    }

    // Verifies that empty training datasets are rejected.
    [Fact]
    public void OptimizeRejectsEmptyTrainingDataset()
    {
        var split = new DatasetSplit(
            [],
            _fixture.Split.Validation,
            _fixture.Split.Testing);

        Assert.Throws<ArgumentException>(() =>
            HyperparameterOptimizer.Optimize(split));
    }

    // Verifies that empty validation datasets are rejected.
    [Fact]
    public void OptimizeRejectsEmptyValidationDataset()
    {
        var split = new DatasetSplit(
            _fixture.Split.Training,
            [],
            _fixture.Split.Testing);

        Assert.Throws<ArgumentException>(() =>
            HyperparameterOptimizer.Optimize(split));
    }
}