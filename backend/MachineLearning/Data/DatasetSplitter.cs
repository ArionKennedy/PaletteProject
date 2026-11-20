using System;
using System.Collections.Generic;
using System.Linq;

namespace Palette.Api.MachineLearning.Data;

public static class DatasetSplitter
{
    // Shuffles ratings into training, validation, and testing sets.
    // Expands training as needed to cover every user and media item.
    public static DatasetSplit Split(
        IReadOnlyList<RatingObservation> ratings,
        double trainingRatio = 0.8,
        double validationRatio = 0.1,
        int seed = 42)
    {
        ValidateInputs(
            ratings,
            trainingRatio,
            validationRatio);

        var shuffledRatings = ShuffleRatings(ratings, seed);

        var (trainingCount, validationCount, testingCount) =
            CalculateSplitSizes(
                ratings.Count,
                trainingRatio,
                validationRatio);

        var (training, remaining) =
            EnsureTrainingCoverage(shuffledRatings);

        FillTrainingSet(training, remaining, trainingCount);

        var validation = remaining
            .Take(validationCount)
            .ToList();

        var testing = remaining
            .Skip(validationCount)
            .Take(testingCount)
            .ToList();

        return new DatasetSplit(
            training,
            validation,
            testing);
    }

    // Validates ratings, split ratios, and unique user-media pairs.
    private static void ValidateInputs(
        IReadOnlyList<RatingObservation> ratings,
        double trainingRatio,
        double validationRatio)
    {
        ArgumentNullException.ThrowIfNull(ratings);

        if (ratings.Count == 0)
        {
            throw new ArgumentException(
                "Ratings cannot be empty.",
                nameof(ratings));
        }

        if (!double.IsFinite(trainingRatio) ||
            !double.IsFinite(validationRatio) ||
            trainingRatio <= 0 ||
            validationRatio <= 0 ||
            trainingRatio + validationRatio >= 1)
        {
            throw new ArgumentException(
                "Invalid split ratios.");
        }

        ValidateUniqueUserMediaPairs(ratings);
    }

    // Rejects duplicate user-media rating pairs.
    private static void ValidateUniqueUserMediaPairs(
    IReadOnlyList<RatingObservation> ratings)
    {
        var knownPairs = new HashSet<(Guid UserId, Guid MediaId)>();

        foreach (var rating in ratings)
        {
            var pair = (rating.UserId, rating.MediaId);

            if (!knownPairs.Add(pair))
            {
                throw new ArgumentException(
                    $"Duplicate user-media pair detected: {pair}",
                    nameof(ratings));
            }
        }
    }

    // Shuffles a copy of the observations using the supplied seed.
    private static RatingObservation[] ShuffleRatings(
        IReadOnlyList<RatingObservation> ratings,
        int seed)
    {
        var random = new Random(seed);
        var shuffled = ratings.ToArray();

        random.Shuffle(shuffled);

        return shuffled;
    }

    // Calculates non-empty target sizes for all three subsets.
    private static (int Training, int Validation, int Testing)
        CalculateSplitSizes(
            int totalCount,
            double trainingRatio,
            double validationRatio)
    {
        int training = (int)(totalCount * trainingRatio);

        int validation = (int)(totalCount * validationRatio);

        int testing = totalCount - training - validation;

        if (training == 0 || validation == 0 || testing == 0)
        {
            throw new ArgumentException(
                "Insufficient ratings for the requested split.");
        }

        return (training, validation, testing);
    }

    // Moves observations into training until all users and media are covered.
    private static (
        List<RatingObservation> Training,
        List<RatingObservation> Remaining)
        EnsureTrainingCoverage(
            IReadOnlyList<RatingObservation> ratings)
    {
        var training = new List<RatingObservation>();
        var remaining = new List<RatingObservation>();

        var knownUsers = new HashSet<Guid>();
        var knownMedia = new HashSet<Guid>();

        foreach (var rating in ratings)
        {
            if (!knownUsers.Contains(rating.UserId) ||
                !knownMedia.Contains(rating.MediaId))
            {
                training.Add(rating);

                knownUsers.Add(rating.UserId);
                knownMedia.Add(rating.MediaId);
            }
            else
            {
                remaining.Add(rating);
            }
        }

        return (training, remaining);
    }

    // Fills the training target from remaining observations.
    private static void FillTrainingSet(
        List<RatingObservation> training,
        List<RatingObservation> remaining,
        int trainingCount)
    {
        if (training.Count > trainingCount)
        {
            throw new InvalidOperationException(
                "Unable to guarantee coverage with the requested training ratio.");
        }

        int additionalCount = trainingCount - training.Count;

        training.AddRange(remaining.Take(additionalCount));

        remaining.RemoveRange(0, additionalCount);
    }
}