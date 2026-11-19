
using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class ContentBasedModelTests
{
    private readonly ContentBasedModel _model = new();

    // Creates a configurable user preference profile.
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
            Guid.NewGuid(), genres ?? new(), authors ?? new(),
            directors ?? new(), studios ?? new(), publishers ?? new(),
            runtime, pageCount, seasons);
    }

    // Creates configurable media features for testing.
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
            Guid.NewGuid(), mediaType,
            genres ?? [], authors ?? [], directors ?? [],
            studios ?? [], publishers ?? [],
            runtime, pageCount, seasons);
    }

    // Basic predictions

    // Verifies that a perfect movie match produces a score of one.
    [Fact]
    public void PredictPerfectMovieMatchReturnsOne()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f },
            studios: new() { ["Warner Bros."] = 2f },
            runtime: 150f);

        var candidate = CreateMedia(
            genres: ["Action"], directors: ["Christopher Nolan"],
            studios: ["Warner Bros."], runtime: 150);

        Assert.Equal(1f, _model.Predict(profile, candidate), 6);
    }

    // Verifies that unrelated media produce a score of zero.
    [Fact]
    public void PredictUnrelatedMediaReturnsZero()
    {
        var profile = CreateProfile(genres: new() { ["Action"] = 5f });
        var candidate = CreateMedia(genres: ["Romance"]);

        Assert.Equal(0f, _model.Predict(profile, candidate));
    }

    // Verifies that an empty user profile produces a score of zero.
    [Fact]
    public void PredictEmptyProfileReturnsZero()
    {
        var candidate = CreateMedia(genres: ["Action"], runtime: 120);

        Assert.Equal(0f, _model.Predict(CreateProfile(), candidate));
    }

    // Verifies that a candidate without features produces a score of zero.
    [Fact]
    public void PredictEmptyCandidateReturnsZero()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f }, runtime: 120f);

        Assert.Equal(0f, _model.Predict(profile, CreateMedia()));
    }

    // Verifies that partially matching genres produce the expected similarity.
    [Fact]
    public void PredictPartialGenreMatchReturnsExpectedSimilarity()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 1f, ["Drama"] = 1f });
        var candidate = CreateMedia(genres: ["Action", "Adventure"]);

        Assert.Equal(0.5f, _model.Predict(profile, candidate), 6);
    }

    // Weighted score calculations

    // Verifies that genre and director scores are correctly combined.
    [Fact]
    public void PredictCombinesGenreAndDirectorWeights()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: ["Action"], directors: ["Steven Spielberg"]);

        Assert.Equal(0.40f / (0.40f + 0.15f),
            _model.Predict(profile, candidate), 6);
    }

    // Verifies that genre and Gaussian length scores are correctly combined.
    [Fact]
    public void PredictCombinesGenreAndLengthScores()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f }, runtime: 120f);
        var candidate = CreateMedia(genres: ["Action"], runtime: 165);

        double lengthScore = Math.Exp(-0.5);
        float expected = (float)((0.40 + 0.15 * lengthScore) / (0.40 + 0.15));

        Assert.Equal(expected, _model.Predict(profile, candidate), 6);
    }

    // Verifies that custom feature weights influence predictions.
    [Fact]
    public void PredictRespectsCustomFeatureWeights()
    {
        var model = new ContentBasedModel(
            new ContentBasedWeights(Genre: 0.8f, Director: 0.2f));

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: ["Action"], directors: ["Steven Spielberg"]);

        Assert.Equal(0.8f, model.Predict(profile, candidate), 6);
    }

    // Verifies that zero-weight features do not influence predictions.
    [Fact]
    public void PredictZeroWeightedFeatureDoesNotAffectScore()
    {
        var model = new ContentBasedModel(
            new ContentBasedWeights(Genre: 1f, Director: 0f));

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: ["Action"], directors: ["Steven Spielberg"]);

        Assert.Equal(1f, model.Predict(profile, candidate), 6);
    }

    // Verifies that wider runtime tolerances increase similarity.
    [Fact]
    public void PredictCustomRuntimeToleranceChangesScore()
    {
        var narrow = new ContentBasedModel(
            new ContentBasedWeights(RuntimeTolerance: 15f));
        var wide = new ContentBasedModel(
            new ContentBasedWeights(RuntimeTolerance: 60f));

        var profile = CreateProfile(runtime: 120f);
        var candidate = CreateMedia(runtime: 180);

        Assert.True(wide.Predict(profile, candidate) > narrow.Predict(profile, candidate));
    }

    // Media-specific predictions

    // Verifies that book recommendations use author and publisher preferences.
    [Fact]
    public void PredictBookUsesAuthorAndPublisher()
    {
        var profile = CreateProfile(
            authors: new() { ["J.R.R. Tolkien"] = 5f },
            publishers: new() { ["HarperCollins"] = 3f });

        var candidate = CreateMedia("book",
            authors: ["J.R.R. Tolkien"], publishers: ["Penguin"]);

        Assert.Equal(0.15f / (0.15f + 0.05f),
            _model.Predict(profile, candidate), 6);
    }

    // Verifies that book recommendations use page count similarity.
    [Fact]
    public void PredictBookUsesPageCount()
    {
        Assert.Equal(0.606531f, _model.Predict(
            CreateProfile(pageCount: 300f),
            CreateMedia("book", pageCount: 400)), 6);
    }

    // Verifies that TV recommendations use season count similarity.
    [Fact]
    public void PredictTvShowUsesSeasonCount()
    {
        Assert.Equal(0.606531f, _model.Predict(
            CreateProfile(seasons: 3f),
            CreateMedia("tv", seasons: 5)), 6);
    }

    // Verifies that game recommendations use studio preferences.
    [Fact]
    public void PredictGameUsesStudioPreferences()
    {
        var profile = CreateProfile(studios: new() { ["FromSoftware"] = 5f });
        var candidate = CreateMedia("game", studios: ["FromSoftware"]);

        Assert.Equal(1f, _model.Predict(profile, candidate), 6);
    }

    // Verifies that movie recommendations ignore book-specific features.
    [Fact]
    public void PredictMovieIgnoresBookSpecificFeatures()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            authors: new() { ["Author A"] = 3f },
            publishers: new() { ["Publisher A"] = 2f },
            pageCount: 300f);

        var candidate = CreateMedia(
            genres: ["Action"], authors: ["Author B"],
            publishers: ["Publisher B"], pageCount: 900);

        Assert.Equal(1f, _model.Predict(profile, candidate), 6);
    }

    // Verifies that media types are normalized before prediction.
    [Fact]
    public void PredictNormalizesMediaType()
    {
        Assert.Equal(0.606531f, _model.Predict(
            CreateProfile(runtime: 120f),
            CreateMedia(" MOVIE ", runtime: 165)), 6);
    }

    // Missing metadata

    // Verifies that missing candidate directors are excluded from scoring.
    [Fact]
    public void PredictMissingCandidateDirectorExcludesWeight()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        Assert.Equal(1f, _model.Predict(
            profile, CreateMedia(genres: ["Action"])), 6);
    }

    // Verifies that missing user director preferences are excluded from scoring.
    [Fact]
    public void PredictMissingUserDirectorPreferenceExcludesWeight()
    {
        var profile = CreateProfile(genres: new() { ["Action"] = 5f });
        var candidate = CreateMedia(
            genres: ["Action"], directors: ["Christopher Nolan"]);

        Assert.Equal(1f, _model.Predict(profile, candidate), 6);
    }

    // Verifies that missing candidate lengths are excluded from scoring.
    [Fact]
    public void PredictMissingCandidateLengthExcludesWeight()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f }, runtime: 120f);

        Assert.Equal(1f, _model.Predict(
            profile, CreateMedia(genres: ["Action"])), 6);
    }

    // Verifies that missing preferred lengths are excluded from scoring.
    [Fact]
    public void PredictMissingUserLengthExcludesWeight()
    {
        var profile = CreateProfile(genres: new() { ["Action"] = 5f });
        var candidate = CreateMedia(genres: ["Action"], runtime: 120);

        Assert.Equal(1f, _model.Predict(profile, candidate), 6);
    }

    // Verifies that blank candidate features are ignored.
    [Fact]
    public void PredictBlankCandidateFeaturesAreIgnored()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f });

        var candidate = CreateMedia(
            genres: ["Action"], directors: ["", " ", "\t"]);

        Assert.Equal(1f, _model.Predict(profile, candidate), 6);
    }

    // Configuration validation

    // Verifies that negative and nonfinite feature weights are rejected.
    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ConstructorInvalidFeatureWeightThrowsException(float weight)
    {
        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(new ContentBasedWeights(Genre: weight)));
    }

    // Verifies that at least one feature must have a positive weight.
    [Fact]
    public void ConstructorAllZeroWeightsThrowsException()
    {
        var weights = new ContentBasedWeights(
            Genre: 0f, Author: 0f, Director: 0f,
            Studio: 0f, Publisher: 0f, Length: 0f);

        Assert.Throws<ArgumentException>(() => new ContentBasedModel(weights));
    }

    // Verifies that nonpositive and nonfinite runtime tolerances are rejected.
    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void ConstructorInvalidRuntimeToleranceThrowsException(float tolerance)
    {
        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(new ContentBasedWeights(RuntimeTolerance: tolerance)));
    }

    // Verifies that page and season count tolerances must be positive.
    [Fact]
    public void ConstructorInvalidOtherTolerancesThrowsException()
    {
        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(new ContentBasedWeights(PageCountTolerance: 0f)));

        Assert.Throws<ArgumentException>(() =>
            new ContentBasedModel(new ContentBasedWeights(SeasonCountTolerance: 0f)));
    }

    // Input validation

    // Verifies that null user profiles are rejected.
    [Fact]
    public void PredictNullProfileThrowsArgumentNullException()
    {
        var candidate = CreateMedia();

        Assert.Throws<ArgumentNullException>(() => _model.Predict(null!, candidate));
    }

    // Verifies that null recommendation candidates are rejected.
    [Fact]
    public void PredictNullCandidateThrowsArgumentNullException()
    {
        var profile = CreateProfile();

        Assert.Throws<ArgumentNullException>(() => _model.Predict(profile, null!));
    }

    // Algorithm properties

    // Verifies that relevant media receive higher scores than unrelated media.
    [Fact]
    public void PredictRelevantMediaRanksHigherThanUnrelatedMedia()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f },
            runtime: 150f);

        var relevant = CreateMedia(
            genres: ["Action"], directors: ["Christopher Nolan"], runtime: 155);

        var unrelated = CreateMedia(
            genres: ["Romance"], directors: ["Other Director"], runtime: 90);

        Assert.True(_model.Predict(profile, relevant) >
            _model.Predict(profile, unrelated));
    }

    // Verifies that 100 repeated predictions produce identical scores.
    [Fact]
    public void PredictRepeatedCallsReturnSameScore()
    {
        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f }, runtime: 120f);

        var candidate = CreateMedia(genres: ["Action", "Drama"], runtime: 150);
        float expected = _model.Predict(profile, candidate);

        for (int i = 0; i < 100; i++)
            Assert.Equal(expected, _model.Predict(profile, candidate));
    }

    // Verifies that proportionally scaling feature weights preserves predictions.
    [Fact]
    public void PredictUniformlyScaledFeatureWeightsPreserveScore()
    {
        var original = new ContentBasedModel(new ContentBasedWeights(
            Genre: 0.4f, Director: 0.15f, Length: 0.15f));

        var scaled = new ContentBasedModel(new ContentBasedWeights(
            Genre: 4f, Author: 1.5f, Director: 1.5f,
            Studio: 1f, Publisher: 0.5f, Length: 1.5f));

        var profile = CreateProfile(
            genres: new() { ["Action"] = 5f },
            directors: new() { ["Christopher Nolan"] = 3f },
            runtime: 120f);

        var candidate = CreateMedia(
            genres: ["Action"], directors: ["Other Director"], runtime: 150);

        Assert.Equal(original.Predict(profile, candidate),
            scaled.Predict(profile, candidate), 6);
    }

    // Verifies that 1,000 randomized predictions remain finite and within [0, 1].
    [Fact]
    public void PredictRandomValidInputsRemainInRange()
    {
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
                genres: [random.Next(2) == 0 ? "Action" : "Romance"],
                runtime: random.Next(60, 240));

            float result = _model.Predict(profile, candidate);

            Assert.True(float.IsFinite(result));
            Assert.InRange(result, 0f, 1f);
        }
    }

    // Verifies that generated preference profiles rank matching media higher.
    [Fact]
    public void PredictGeneratedProfileRanksMatchingMediaHigher()
    {
        var history = new[]
        {
            new WeightedMediaInteraction(CreateMedia(
                genres: ["Action"], directors: ["Christopher Nolan"], runtime: 150), 5f),

            new WeightedMediaInteraction(CreateMedia(
                genres: ["Action"], directors: ["Christopher Nolan"], runtime: 160), 3f)
        };

        var profile = UserProfileBuilder.Build(Guid.NewGuid(), history);

        var relevant = CreateMedia(
            genres: ["Action"], directors: ["Christopher Nolan"], runtime: 155);

        var unrelated = CreateMedia(
            genres: ["Romance"], directors: ["Other Director"], runtime: 90);

        Assert.True(_model.Predict(profile, relevant) >
            _model.Predict(profile, unrelated));
    }
}