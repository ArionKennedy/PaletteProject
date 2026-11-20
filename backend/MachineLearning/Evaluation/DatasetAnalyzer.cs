using System;
using System.Collections.Generic;
using System.Linq;

namespace Palette.Api.MachineLearning.Evaluation;

public static class DatasetAnalyzer
{
    // Prints dataset size, sparsity, rating, and activity statistics.
    public static void Analyze(
        IReadOnlyList<RatingObservation> ratings)
    {
        ArgumentNullException.ThrowIfNull(ratings);

        if (ratings.Count == 0)
            throw new ArgumentException(
                "Ratings cannot be empty.", nameof(ratings));

        var userGroups = ratings
            .GroupBy(r => r.UserId)
            .ToList();

        var mediaGroups = ratings
            .GroupBy(r => r.MediaId)
            .ToList();

        int userCount = userGroups.Count;
        int mediaCount = mediaGroups.Count;

        long possibleRatings = (long)userCount * mediaCount;

        int uniquePairs = ratings
            .Select(r => (r.UserId, r.MediaId))
            .Distinct()
            .Count();

        double density = (double)uniquePairs / possibleRatings;
        double sparsity = 1 - density;

        var ratingValues = ratings
            .Select(r => (double)r.Rating)
            .ToArray();

        Console.WriteLine("DATASET ANALYSIS");

        Console.WriteLine($"\nDATASET OVERVIEW");
        Console.WriteLine($"Users: {userCount}");
        Console.WriteLine($"Media: {mediaCount}");
        Console.WriteLine($"Total ratings: {ratings.Count}");
        Console.WriteLine($"Possible ratings: {possibleRatings}");
        Console.WriteLine($"Density: {density:P2}");
        Console.WriteLine($"Sparsity: {sparsity:P2}");

        PrintStatistics("RATING STATISTICS", ratingValues);

        PrintRatingDistribution(ratings);

        PrintStatistics(
            "RATINGS PER USER",
            userGroups.Select(g => (double)g.Count()).ToArray()
        );

        PrintStatistics(
            "RATINGS PER MEDIA",
            mediaGroups.Select(g => (double)g.Count()).ToArray()
        );

        PrintStatistics(
            "USER MEAN RATINGS",
            userGroups.Select(
                g => g.Average(r => (double)r.Rating)
            ).ToArray()
        );

        PrintStatistics(
            "MEDIA MEAN RATINGS",
            mediaGroups.Select(
                g => g.Average(r => (double)r.Rating)
            ).ToArray()
        );
    }

    // Prints the count and proportion of ratings at each star level.
    private static void PrintRatingDistribution(
        IReadOnlyList<RatingObservation> ratings)
    {
        Console.WriteLine("\nRATING DISTRIBUTION");

        for (int rating = 1; rating <= 5; rating++)
        {
            int count = ratings.Count(
                r => Math.Round(
                    r.Rating,
                    MidpointRounding.AwayFromZero
                ) == rating
            );

            double percentage =
                (double)count / ratings.Count * 100;

            string bar = new string(
                '#', (int)Math.Round(percentage / 2)
            );

            Console.WriteLine(
                $"{rating} Star | {count,5} | " +
                $"{percentage,6:F2}% | {bar}"
            );
        }
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

    // Prints the mean, standard deviation, minimum, and maximum.
    private static void PrintStatistics(
        string name,
        IReadOnlyList<double> values)
    {
        Console.WriteLine($"\n{name}");
        Console.WriteLine($"Mean: {values.Average():F6}");

        Console.WriteLine(
            $"Standard deviation: " +
            $"{CalculateStandardDeviation(values):F6}"
        );

        Console.WriteLine($"Minimum: {values.Min():F6}");
        Console.WriteLine($"Maximum: {values.Max():F6}");
    }
}