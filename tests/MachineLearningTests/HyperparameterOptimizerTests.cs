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

    public HyperparameterOptimizerFixture()
    {
        var ratings = DataGenerator.Generate(
            userCount: 10,
            movieCount: 20,
            ratingsPerUser: 10,
            seed: 42
        );

        Split = DatasetSplitter.Split(ratings);

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

    private IReadOnlyList<OptimizationResult> Results =>
        _fixture.Results;

    public HyperparameterOptimizerTests(
        HyperparameterOptimizerFixture fixture)
    {
        _fixture = fixture;
    }

    // Configuration generation

    [Fact]
    public void OptimizeReturnsAll480Configurations()
    {
        Assert.Equal(480, Results.Count);
    }

    [Fact]
    public void OptimizeReturnsUniqueConfigurations()
    {
        Assert.Equal(
            Results.Count,
            Results.Select(r => r.Parameters).Distinct().Count()
        );
    }

    [Fact]
    public void OptimizeEvaluatesAllFactorCounts()
    {
        int[] expected = { 1, 2, 3, 4, 8, 16 };

        var actual = Results
            .Select(r => r.Parameters.FactorCount)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OptimizeEvaluatesAllEpochCounts()
    {
        int[] expected = { 25, 50, 100, 200 };

        var actual = Results
            .Select(r => r.Parameters.Epochs)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OptimizeEvaluatesAllLearningRates()
    {
        float[] expected =
        {
            0.001f, 0.005f, 0.01f, 0.02f
        };

        var actual = Results
            .Select(r => r.Parameters.LearningRate)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OptimizeEvaluatesAllRegularizations()
    {
        float[] expected =
        {
            0f, 0.01f, 0.02f, 0.05f, 0.1f
        };

        var actual = Results
            .Select(r => r.Parameters.Regularization)
            .Distinct()
            .OrderBy(v => v)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OptimizeIncludesOriginalConfiguration()
    {
        var original = new Hyperparameters(
            8, 100, 0.01f, 0.02f
        );

        Assert.Contains(
            Results,
            r => r.Parameters == original
        );
    }

    [Fact]
    public void OptimizeIncludesPreviouslyOptimalConfiguration()
    {
        var optimized = new Hyperparameters(
            2, 100, 0.02f, 0.02f
        );

        Assert.Contains(
            Results,
            r => r.Parameters == optimized
        );
    }

    // Result validation

    [Fact]
    public void OptimizeReturnsNonNegativeTrainingRmse()
    {
        Assert.All(Results, result =>
            Assert.True(result.TrainingRmse >= 0));
    }

    [Fact]
    public void OptimizeReturnsNonNegativeValidationRmse()
    {
        Assert.All(Results, result =>
            Assert.True(result.ValidationRmse >= 0));
    }

    [Fact]
    public void OptimizeReturnsFiniteTrainingRmse()
    {
        Assert.All(Results, result =>
            Assert.True(double.IsFinite(result.TrainingRmse)));
    }

    [Fact]
    public void OptimizeReturnsFiniteValidationRmse()
    {
        Assert.All(Results, result =>
            Assert.True(double.IsFinite(result.ValidationRmse)));
    }

    [Fact]
    public void OptimizeReturnsNonNegativeTrainingTime()
    {
        Assert.All(Results, result =>
            Assert.True(result.TrainingTimeMs >= 0));
    }

    [Fact]
    public void OptimizeReturnsFiniteTrainingTime()
    {
        Assert.All(Results, result =>
            Assert.True(double.IsFinite(result.TrainingTimeMs)));
    }

    // Model evaluation

    [Fact]
    public void OptimizeCalculatesCorrectTrainingRmse()
    {
        var result = Results.First();

        var model = TrainModel(result.Parameters);

        double expected = ModelEvaluator.CalculateRmse(
            _fixture.Split.Training,
            model.Predict
        );

        Assert.Equal(expected, result.TrainingRmse, 6);
    }

    [Fact]
    public void OptimizeCalculatesCorrectValidationRmse()
    {
        var result = Results.First();

        var model = TrainModel(result.Parameters);

        double expected = ModelEvaluator.CalculateRmse(
            _fixture.Split.Validation,
            model.Predict
        );

        Assert.Equal(expected, result.ValidationRmse, 6);
    }

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
                _fixture.Split.Validation,
                model.Predict
            );

            Assert.Equal(expected, result.ValidationRmse, 6);
        }
    }

    // Sorting

    [Fact]
    public void OptimizeSortsByAscendingValidationRmse()
    {
        for (int i = 1; i < Results.Count; i++)
        {
            Assert.True(
                Results[i - 1].ValidationRmse <=
                Results[i].ValidationRmse
            );
        }
    }

    [Fact]
    public void OptimizeReturnsBestConfigurationFirst()
    {
        double minimum = Results.Min(
            r => r.ValidationRmse
        );

        Assert.Equal(
            minimum,
            Results.First().ValidationRmse
        );
    }

    // Reproducibility

    [Fact]
    public void OptimizeProducesConsistentBestConfiguration()
    {
        var repeated = HyperparameterOptimizer.Optimize(
            _fixture.Split
        );

        Assert.Equal(
            Results.First().Parameters,
            repeated.First().Parameters
        );
    }

    [Fact]
    public void OptimizeProducesConsistentRmseValues()
    {
        var repeated = HyperparameterOptimizer.Optimize(
            _fixture.Split
        );

        Assert.Equal(Results.Count, repeated.Count);

        for (int i = 0; i < Results.Count; i++)
        {
            Assert.Equal(
                Results[i].Parameters,
                repeated[i].Parameters
            );

            Assert.Equal(
                Results[i].TrainingRmse,
                repeated[i].TrainingRmse,
                6
            );

            Assert.Equal(
                Results[i].ValidationRmse,
                repeated[i].ValidationRmse,
                6
            );
        }
    }

    // Data integrity

    [Fact]
    public void OptimizeDoesNotModifyTrainingDataset()
    {
        Assert.Equal(
            _fixture.OriginalTraining,
            _fixture.Split.Training.ToArray()
        );
    }

    [Fact]
    public void OptimizeDoesNotModifyValidationDataset()
    {
        Assert.Equal(
            _fixture.OriginalValidation,
            _fixture.Split.Validation.ToArray()
        );
    }

    [Fact]
    public void OptimizeDoesNotModifyTestingDataset()
    {
        Assert.Equal(
            _fixture.OriginalTesting,
            _fixture.Split.Testing.ToArray()
        );
    }

    [Fact]
    public void OptimizeDoesNotDependOnTestingDataset()
    {
        var modifiedSplit = new DatasetSplit(
            _fixture.Split.Training,
            _fixture.Split.Validation,
            Array.Empty<RatingObservation>()
        );

        var repeated = HyperparameterOptimizer.Optimize(
            modifiedSplit
        );

        Assert.Equal(Results.Count, repeated.Count);

        for (int i = 0; i < Results.Count; i++)
        {
            Assert.Equal(
                Results[i].Parameters,
                repeated[i].Parameters
            );

            Assert.Equal(
                Results[i].TrainingRmse,
                repeated[i].TrainingRmse,
                6
            );

            Assert.Equal(
                Results[i].ValidationRmse,
                repeated[i].ValidationRmse,
                6
            );
        }
    }

    // Input validation

    [Fact]
    public void OptimizeRejectsNullDatasetSplit()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HyperparameterOptimizer.Optimize(null!));
    }

    [Fact]
    public void OptimizeRejectsEmptyTrainingDataset()
    {
        var split = new DatasetSplit(
            Array.Empty<RatingObservation>(),
            _fixture.Split.Validation,
            _fixture.Split.Testing
        );

        Assert.Throws<ArgumentException>(() =>
            HyperparameterOptimizer.Optimize(split));
    }

    [Fact]
    public void OptimizeRejectsEmptyValidationDataset()
    {
        var split = new DatasetSplit(
            _fixture.Split.Training,
            Array.Empty<RatingObservation>(),
            _fixture.Split.Testing
        );

        Assert.Throws<ArgumentException>(() =>
            HyperparameterOptimizer.Optimize(split));
    }

    // Helper methods

    private MatrixFactorizationModel TrainModel(
        Hyperparameters parameters)
    {
        var model = new MatrixFactorizationModel(
            parameters.FactorCount
        );

        model.Train(
            _fixture.Split.Training,
            epochs: parameters.Epochs,
            learningRate: parameters.LearningRate,
            regularization: parameters.Regularization
        );

        return model;
    }
}