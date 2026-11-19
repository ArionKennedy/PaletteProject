
using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class ContentBasedModelTests
{
    // Creates a user-preference profile fixture.
    private static UserPreferenceProfile CreateProfile(
        Dictionary<string, float>? genres = null,
        Dictionary<string, float>? authors = null,
        Dictionary<string, float>? directors = null,
        Dictionary<string, float>? studios = null,
        Dictionary<string, float>? publishers = null,
        float? runtime = null,
        float? pageCount = null,
        float? seasons = null)
    {
        return new UserPreferenceProfile(
            UserId: Guid.NewGuid(),
            GenrePreferences: genres ?? new(),
            AuthorPreferences: authors ?? new(),
            DirectorPreferences: directors ?? new(),
            StudioPreferences: studios ?? new(),
            PublisherPreferences: publishers ?? new(),
            PreferredRuntimeMinutes: runtime,
            PreferredPageCount: pageCount,
            PreferredNumberOfSeasons: seasons
        );
    }

    // Creates media-feature test data.
    private static MediaFeatures CreateMedia(
        string mediaType = "movie",
        string[]? genres = null,
        string[]? authors = null,
        string[]? directors = null,
        string[]? studios = null,
        string[]? publishers = null,
        int? runtime = null,
        int? pageCount = null,
        int? seasons = null)
    {
        return new MediaFeatures(
            MediaId: Guid.NewGuid(),
            MediaType: mediaType,
            Genres: genres ?? Array.Empty<string>(),
            Authors: authors ?? Array.Empty<string>(),
            Directors: directors ?? Array.Empty<string>(),
            Studios: studios ?? Array.Empty<string>(),
            Publishers: publishers ?? Array.Empty<string>(),
            RuntimeMinutes: runtime,
            PageCount: pageCount,
            NumberOfSeasons: seasons
        );
    }

    // ==================================================
    // Basic Prediction Tests
    // ==================================================

    [Fact]
    // Verifies that predicting with perfect movie match returns one.
    public void PredictPerfectMovieMatchReturnsOne()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f },
            studios: new() { ["Warner Bros."] = 2f },
            runtime: 150f);

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Christopher Nolan" },
            studios: new[] { "Warner Bros." },
            runtime: 150);

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with unrelated media returns zero.
    public void PredictUnrelatedMediaReturnsZero()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f });

        var candidate = CreateMedia(
            genres: new[] { "Romance" });

        Assert.Equal(0f, model.Predict(profile, candidate));
    }

    [Fact]
    // Verifies that predicting with empty profile returns zero.
    public void PredictEmptyProfileReturnsZero()
    {
        var model = new ContentBasedModel();
        var profile = CreateProfile();

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            runtime: 120);

        Assert.Equal(0f, model.Predict(profile, candidate));
    }

    [Fact]
    // Verifies that predicting with empty candidate returns zero.
    public void PredictEmptyCandidateReturnsZero()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            runtime: 120f);

        var candidate = CreateMedia();

        Assert.Equal(0f, model.Predict(profile, candidate));
    }

    [Fact]
    // Verifies that predicting with partial genre match returns the expected similarity.
    public void PredictPartialGenreMatchReturnsExpectedSimilarity()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new()
            {
                ["Action"] = 1f,
                ["Drama"] = 1f
            });

        var candidate = CreateMedia(
            genres: new[] { "Action", "Adventure" });

        Assert.Equal(0.5f, model.Predict(profile, candidate), 6);
    }

    // ==================================================
    // Weighted Score Calculation Tests
    // ==================================================

    [Fact]
    // Verifies that predicting combines genre and director weights.
    public void PredictCombinesGenreAndDirectorWeights()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Steven Spielberg" });

        float expected = 0.40f / (0.40f + 0.15f);

        Assert.Equal(expected, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting combines genre and length scores.
    public void PredictCombinesGenreAndLengthScores()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            runtime: 120f);

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            runtime: 165);

        double lengthScore = Math.Exp(-0.5);

        float expected = (float)(
            (0.40 * 1.0 + 0.15 * lengthScore)
            / (0.40 + 0.15));

        Assert.Equal(expected, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting respects custom feature weights.
    public void PredictRespectsCustomFeatureWeights()
    {
        var weights = new ContentBasedWeights(
            Genre: 0.8f,
            Director: 0.2f);

        var model = new ContentBasedModel(weights);

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Steven Spielberg" });

        Assert.Equal(0.8f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with zero weighted feature does not affect score.
    public void PredictZeroWeightedFeatureDoesNotAffectScore()
    {
        var weights = new ContentBasedWeights(
            Genre: 1f,
            Director: 0f);

        var model = new ContentBasedModel(weights);

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Steven Spielberg" });

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with custom runtime tolerance changes score.
    public void PredictCustomRuntimeToleranceChangesScore()
    {
        var narrowModel = new ContentBasedModel(
            new ContentBasedWeights(RuntimeTolerance: 15f));

        var wideModel = new ContentBasedModel(
            new ContentBasedWeights(RuntimeTolerance: 60f));

        var profile = CreateProfile(runtime: 120f);
        var candidate = CreateMedia(runtime: 180);

        Assert.True(
            wideModel.Predict(profile, candidate) >
            narrowModel.Predict(profile, candidate));
    }

    // ==================================================
    // Media-Specific Prediction Tests
    // ==================================================

    [Fact]
    // Verifies that predicting with book uses author and publisher.
    public void PredictBookUsesAuthorAndPublisher()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            authors: new() { ["J.R.R. Tolkien"] = 5f },
            publishers: new() { ["HarperCollins"] = 3f });

        var candidate = CreateMedia(
            mediaType: "book",
            authors: new[] { "J.R.R. Tolkien" },
            publishers: new[] { "Penguin" });

        float expected = 0.15f / (0.15f + 0.05f);

        Assert.Equal(expected, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with book uses page count.
    public void PredictBookUsesPageCount()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(pageCount: 300f);

        var candidate = CreateMedia(
            mediaType: "book",
            pageCount: 400);

        Assert.Equal(
            0.606531f,
            model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with tv show uses season count.
    public void PredictTvShowUsesSeasonCount()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(seasons: 3f);

        var candidate = CreateMedia(
            mediaType: "tv",
            seasons: 5);

        Assert.Equal(
            0.606531f,
            model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with game uses studio preferences.
    public void PredictGameUsesStudioPreferences()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            studios: new() { ["FromSoftware"] = 5f });

        var candidate = CreateMedia(
            mediaType: "game",
            studios: new[] { "FromSoftware" });

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with movie ignores book specific features.
    public void PredictMovieIgnoresBookSpecificFeatures()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            authors: new() { ["Author A"] = 3f },
            publishers: new() { ["Publisher A"] = 2f },
            pageCount: 300f);

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            authors: new[] { "Author B" },
            publishers: new[] { "Publisher B" },
            pageCount: 900);

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies predicting for normalizes media type.
    public void PredictNormalizesMediaType()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(runtime: 120f);

        var candidate = CreateMedia(
            mediaType: " MOVIE ",
            runtime: 165);

        Assert.Equal(
            0.606531f,
            model.Predict(profile, candidate), 6);
    }

    // ==================================================
    // Missing Metadata Tests
    // ==================================================

    [Fact]
    // Verifies that predicting with missing candidate director excludes weight.
    public void PredictMissingCandidateDirectorExcludesWeight()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: new[] { "Action" });

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with missing user director preference excludes weight.
    public void PredictMissingUserDirectorPreferenceExcludesWeight()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f });

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Christopher Nolan" });

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with missing candidate length excludes weight.
    public void PredictMissingCandidateLengthExcludesWeight()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            runtime: 120f);

        var candidate = CreateMedia(
            genres: new[] { "Action" });

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies that predicting with missing user length excludes weight.
    public void PredictMissingUserLengthExcludesWeight()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f });

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            runtime: 120);

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies predicting for blank candidate features are ignored.
    public void PredictBlankCandidateFeaturesAreIgnored()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "", " ", "\t" });

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    // ==================================================
    // Configuration Validation Tests
    // ==================================================

    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that constructing a model with invalid feature weight throws an exception.
    public void ConstructorInvalidFeatureWeightThrowsException(
        float weight)
    {
        var weights = new ContentBasedWeights(Genre: weight);

        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(weights));
    }

    [Fact]
    // Verifies that constructing a model with all zero weights throws an exception.
    public void ConstructorAllZeroWeightsThrowsException()
    {
        var weights = new ContentBasedWeights(
            Genre: 0f,
            Author: 0f,
            Director: 0f,
            Studio: 0f,
            Publisher: 0f,
            Length: 0f);

        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(weights));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that constructing a model with invalid runtime tolerance throws an exception.
    public void ConstructorInvalidRuntimeToleranceThrowsException(
        float tolerance)
    {
        var weights = new ContentBasedWeights(
            RuntimeTolerance: tolerance);

        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(weights));
    }

    [Fact]
    // Verifies that constructing a model with invalid other tolerances throws an exception.
    public void ConstructorInvalidOtherTolerancesThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(
                new ContentBasedWeights(PageCountTolerance: 0f)));

        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(
                new ContentBasedWeights(SeasonCountTolerance: 0f)));
    }

    // ==================================================
    // Invalid Input Tests
    // ==================================================

    [Fact]
    // Verifies that predicting with null profile throws ArgumentNullException.
    public void PredictNullProfileThrowsArgumentNullException()
    {
        var model = new ContentBasedModel();
        var candidate = CreateMedia();

        Assert.Throws<ArgumentNullException>(() =>
            model.Predict(null!, candidate));
    }

    [Fact]
    // Verifies that predicting with null candidate throws ArgumentNullException.
    public void PredictNullCandidateThrowsArgumentNullException()
    {
        var model = new ContentBasedModel();
        var profile = CreateProfile();

        Assert.Throws<ArgumentNullException>(() =>
            model.Predict(profile, null!));
    }

    // ==================================================
    // Algorithm Property Tests
    // ==================================================

    [Fact]
    // Verifies that predicting with relevant media ranks higher than unrelated media.
    public void PredictRelevantMediaRanksHigherThanUnrelatedMedia()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f },
            runtime: 150f);

        var relevant = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Christopher Nolan" },
            runtime: 155);

        var unrelated = CreateMedia(
            genres: new[] { "Romance" },
            directors: new[] { "Other Director" },
            runtime: 90);

        Assert.True(
            model.Predict(profile, relevant) >
            model.Predict(profile, unrelated));
    }

    [Fact]
    // Verifies predicting for repeated calls return same score.
    public void PredictRepeatedCallsReturnSameScore()
    {
        var model = new ContentBasedModel();

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            runtime: 120f);

        var candidate = CreateMedia(
            genres: new[] { "Action", "Drama" },
            runtime: 150);

        float expected = model.Predict(profile, candidate);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(expected, model.Predict(profile, candidate));
        }
    }

    [Fact]
    // Verifies predicting for uniformly scaled feature weights preserve score.
    public void PredictUniformlyScaledFeatureWeightsPreserveScore()
    {
        var original = new ContentBasedModel(
            new ContentBasedWeights(
                Genre: 0.4f,
                Director: 0.15f,
                Length: 0.15f));

        var scaled = new ContentBasedModel(
            new ContentBasedWeights(
                Genre: 4f,
                Author: 1.5f,
                Director: 1.5f,
                Studio: 1f,
                Publisher: 0.5f,
                Length: 1.5f));

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f },
            runtime: 120f);

        var candidate = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Other Director" },
            runtime: 150);

        Assert.Equal(
            original.Predict(profile, candidate),
            scaled.Predict(profile, candidate), 6);
    }

    [Fact]
    // Verifies predicting for random valid inputs remain in range.
    public void PredictRandomValidInputsRemainInRange()
    {
        var model = new ContentBasedModel();
        var random = new Random(42);

        for (int i = 0; i < 1000; i++)
        {
            var profile = CreateProfile(
                genres: new()
                {
                    ["Action"] = (float)random.NextDouble() * 10f,
                    ["Drama"] = (float)random.NextDouble() * 10f
                },
                runtime: random.Next(60, 240));

            var candidate = CreateMedia(
                genres: new[]
                {
                    random.Next(2) == 0 ? "Action" : "Romance"
                },
                runtime: random.Next(60, 240));

            float result = model.Predict(profile, candidate);

            Assert.True(float.IsFinite(result));
            Assert.InRange(result, 0f, 1f);
        }
    }

    [Fact]
    // Verifies that predicting with generated profile ranks matching media higher.
    public void PredictGeneratedProfileRanksMatchingMediaHigher()
    {
        var model = new ContentBasedModel();

        var history = new[]
        {
            new WeightedMediaInteraction(
                CreateMedia(
                    genres: new[] { "Action" },
                    directors: new[] { "Christopher Nolan" },
                    runtime: 150), 5f),

            new WeightedMediaInteraction(
                CreateMedia(
                    genres: new[] { "Action" },
                    directors: new[] { "Christopher Nolan" },
                    runtime: 160), 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), history);

        var relevant = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Christopher Nolan" },
            runtime: 155);

        var unrelated = CreateMedia(
            genres: new[] { "Romance" },
            directors: new[] { "Other Director" },
            runtime: 90);

        Assert.True(
            model.Predict(profile, relevant) >
            model.Predict(profile, unrelated));
    }
}
