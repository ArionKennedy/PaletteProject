namespace Palette.Api.MachineLearning.Data;

public static class DatasetSplitter
{
    public static DatasetSplit Split(
    IReadOnlyList<RatingObservation> ratings,
    double trainingRatio = 0.8,
    double validationRatio = 0.1,
    int seed = 42)
    {
        // Check for empty ratings
        if (ratings.Count == 0)
            throw new ArgumentException("Ratings cannot be empty.");

        // Validate split ratios
        if (!double.IsFinite(trainingRatio) ||
            !double.IsFinite(validationRatio) ||
            trainingRatio <= 0 ||
            validationRatio <= 0 ||
            trainingRatio + validationRatio >= 1)
        {
            throw new ArgumentException("Invalid split ratios.");
        }

        // Generate a random seed
        var random = new Random(seed);
        var shuffledRatings = ratings.ToArray();

        // Shuffle the ratings
        random.Shuffle(shuffledRatings);

        // Find the counts for each split
        int trainingCount = (int)(ratings.Count * trainingRatio);
        int validationCount = (int)(ratings.Count * validationRatio);
        int testingCount = ratings.Count - trainingCount - validationCount;

        // Check if there are enough ratings for the split
        if (trainingCount == 0 ||
            validationCount == 0 ||
            testingCount == 0)
        {
            throw new ArgumentException(
                "Insufficient ratings for the requested split.");
        }

        // Prepare lists for the splits
        var training = new List<RatingObservation>();
        var remaining = new List<RatingObservation>();

        // Track known users and movies to ensure coverage
        var knownUsers = new HashSet<Guid>();
        var knownMovies = new HashSet<Guid>();

        // Guarantee user and movie coverage
        foreach (var rating in shuffledRatings)
        {
            if (!knownUsers.Contains(rating.UserId) ||
                !knownMovies.Contains(rating.MediaId))
            {
                training.Add(rating);

                knownUsers.Add(rating.UserId);
                knownMovies.Add(rating.MediaId);
            }
            else
            {
                remaining.Add(rating);
            }
        }

        // Check if there is enough training data to guarantee coverage
        if (training.Count > trainingCount)
        {
            throw new InvalidOperationException(
                "Unable to guarantee coverage with the requested training ratio.");
        }

        // Fill remaining training capacity
        int additionalTrainingCount = trainingCount - training.Count;

        training.AddRange(
            remaining.Take(additionalTrainingCount));

        // Partition the remaining observations
        var validation = remaining
            .Skip(additionalTrainingCount)
            .Take(validationCount)
            .ToList();

        var testing = remaining
            .Skip(additionalTrainingCount + validationCount)
            .ToList();

        // Return the split dataset
        return new DatasetSplit(training, validation, testing);
    }
}