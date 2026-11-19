
using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class UserProfileBuilderTests
{
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

    // Builds a preference profile from the supplied weighted interactions.
    private static UserPreferenceProfile BuildProfile(
        params WeightedMediaInteraction[] interactions)
    {
        return UserProfileBuilder.Build(Guid.NewGuid(), interactions);
    }

    // Profile initialization

    // Verifies that the generated profile preserves the user's identifier.
    [Fact]
    public void BuildPreservesUserId()
    {
        Guid userId = Guid.NewGuid();

        Assert.Equal(userId, UserProfileBuilder.Build(userId, []).UserId);
    }

    // Verifies that users without interactions receive an empty profile.
    [Fact]
    public void BuildEmptyInteractionsReturnsEmptyProfile()
    {
        var profile = BuildProfile();

        Assert.Empty(profile.GenrePreferences);
        Assert.Empty(profile.AuthorPreferences);
        Assert.Empty(profile.DirectorPreferences);
        Assert.Empty(profile.StudioPreferences);
        Assert.Empty(profile.PublisherPreferences);
        Assert.Null(profile.PreferredRuntimeMinutes);
        Assert.Null(profile.PreferredPageCount);
        Assert.Null(profile.PreferredNumberOfSeasons);
    }

    // Verifies that zero-weight interactions do not influence preferences.
    [Fact]
    public void BuildZeroWeightInteractionDoesNotAffectProfile()
    {
        var media = CreateMedia(genres: ["Action"],
            directors: ["Christopher Nolan"], runtime: 120);

        var profile = BuildProfile(new WeightedMediaInteraction(media, 0f));

        Assert.Empty(profile.GenrePreferences);
        Assert.Empty(profile.DirectorPreferences);
        Assert.Null(profile.PreferredRuntimeMinutes);
    }

    // Categorical preference accumulation

    // Verifies that a single interaction contributes its genre weight.
    [Fact]
    public void BuildSingleInteractionAccumulatesGenreWeight()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: ["Action"]), 3f));

        Assert.Equal(3f, profile.GenrePreferences["Action"]);
    }

    // Verifies that multiple interactions accumulate overlapping genre weights.
    [Fact]
    public void BuildMultipleInteractionsAccumulatesGenreWeights()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: ["Action", "Adventure"]), 3f),
            new WeightedMediaInteraction(CreateMedia(genres: ["Action", "Drama"]), 2f));

        Assert.Equal(5f, profile.GenrePreferences["Action"]);
        Assert.Equal(3f, profile.GenrePreferences["Adventure"]);
        Assert.Equal(2f, profile.GenrePreferences["Drama"]);
    }

    // Verifies that book interactions contribute author preferences.
    [Fact]
    public void BuildAccumulatesAuthorPreferences()
    {
        var media = CreateMedia("book", authors: ["J.R.R. Tolkien"]);
        var profile = BuildProfile(new WeightedMediaInteraction(media, 4f));

        Assert.Equal(4f, profile.AuthorPreferences["J.R.R. Tolkien"]);
    }

    // Verifies that movie interactions contribute director preferences.
    [Fact]
    public void BuildAccumulatesDirectorPreferences()
    {
        var media = CreateMedia(directors: ["Christopher Nolan"]);
        var profile = BuildProfile(new WeightedMediaInteraction(media, 5f));

        Assert.Equal(5f, profile.DirectorPreferences["Christopher Nolan"]);
    }

    // Verifies that media interactions contribute studio preferences.
    [Fact]
    public void BuildAccumulatesStudioPreferences()
    {
        var media = CreateMedia(studios: ["Warner Bros."]);
        var profile = BuildProfile(new WeightedMediaInteraction(media, 3f));

        Assert.Equal(3f, profile.StudioPreferences["Warner Bros."]);
    }

    // Verifies that book interactions contribute publisher preferences.
    [Fact]
    public void BuildAccumulatesPublisherPreferences()
    {
        var media = CreateMedia("book", publishers: ["HarperCollins"]);
        var profile = BuildProfile(new WeightedMediaInteraction(media, 2f));

        Assert.Equal(2f, profile.PublisherPreferences["HarperCollins"]);
    }

    // Verifies that repeated interactions with the same media accumulate weights.
    [Fact]
    public void BuildRepeatedMediaInteractionsAccumulatesWeights()
    {
        var media = CreateMedia(genres: ["Action"]);

        var profile = BuildProfile(
            new WeightedMediaInteraction(media, 2f),
            new WeightedMediaInteraction(media, 3f));

        Assert.Equal(5f, profile.GenrePreferences["Action"]);
    }

    // Feature normalization

    // Verifies that differently capitalized features combine their weights.
    [Fact]
    public void BuildDifferentCapitalizationCombinesPreferences()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: ["ACTION"]), 2f),
            new WeightedMediaInteraction(CreateMedia(genres: ["action"]), 3f));

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(5f, profile.GenrePreferences["Action"]);
    }

    // Verifies that surrounding whitespace does not create separate preferences.
    [Fact]
    public void BuildExtraWhitespaceCombinesPreferences()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: [" Action "]), 2f),
            new WeightedMediaInteraction(CreateMedia(genres: ["Action"]), 3f));

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(5f, profile.GenrePreferences["Action"]);
    }

    // Verifies that duplicate features within one interaction do not inflate weights.
    [Fact]
    public void BuildDuplicateFeaturesDoesNotInflatePreferences()
    {
        var media = CreateMedia(genres: ["Action", "ACTION", " Action "]);
        var profile = BuildProfile(new WeightedMediaInteraction(media, 4f));

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(4f, profile.GenrePreferences["Action"]);
    }

    // Verifies that empty and whitespace-only features are ignored.
    [Fact]
    public void BuildBlankFeaturesAreIgnored()
    {
        var media = CreateMedia(genres: ["", " ", "\t", "Action"]);
        var profile = BuildProfile(new WeightedMediaInteraction(media, 2f));

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(2f, profile.GenrePreferences["Action"]);
    }

    // Weighted length calculations

    // Verifies that a single movie establishes the preferred runtime.
    [Fact]
    public void BuildSingleMovieReturnsItsRuntime()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(runtime: 120), 3f));

        Assert.Equal(120f, profile.PreferredRuntimeMinutes);
    }

    // Verifies that equally weighted runtimes produce the arithmetic mean.
    [Fact]
    public void BuildEqualWeightsReturnsArithmeticMean()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(runtime: 100), 1f),
            new WeightedMediaInteraction(CreateMedia(runtime: 200), 1f));

        Assert.Equal(150f, profile.PreferredRuntimeMinutes);
    }

    // Verifies that differently weighted runtimes produce the weighted mean.
    [Fact]
    public void BuildDifferentWeightsReturnsWeightedMean()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(runtime: 100), 3f),
            new WeightedMediaInteraction(CreateMedia(runtime: 200), 1f));

        Assert.Equal(125f, profile.PreferredRuntimeMinutes);
    }

    // Verifies that book interactions produce a weighted preferred page count.
    [Fact]
    public void BuildBooksReturnsWeightedPageCount()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia("book", pageCount: 200), 1f),
            new WeightedMediaInteraction(CreateMedia("book", pageCount: 400), 3f));

        Assert.Equal(350f, profile.PreferredPageCount);
    }

    // Verifies that TV interactions produce a weighted preferred season count.
    [Fact]
    public void BuildTvShowsReturnsWeightedSeasonCount()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia("tv", seasons: 2), 3f),
            new WeightedMediaInteraction(CreateMedia("tv", seasons: 6), 1f));

        Assert.Equal(3f, profile.PreferredNumberOfSeasons);
    }

    // Verifies that different media types maintain independent length preferences.
    [Fact]
    public void BuildMixedMediaKeepsLengthPreferencesSeparate()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(runtime: 150), 3f),
            new WeightedMediaInteraction(CreateMedia("book", pageCount: 350), 2f),
            new WeightedMediaInteraction(CreateMedia("tv", seasons: 5), 4f));

        Assert.Equal(150f, profile.PreferredRuntimeMinutes);
        Assert.Equal(350f, profile.PreferredPageCount);
        Assert.Equal(5f, profile.PreferredNumberOfSeasons);
    }

    // Verifies that missing lengths do not influence the weighted mean.
    [Fact]
    public void BuildMissingLengthsDoesNotAffectWeightedMean()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(runtime: 120), 1f),
            new WeightedMediaInteraction(CreateMedia(runtime: null), 5f));

        Assert.Equal(120f, profile.PreferredRuntimeMinutes);
    }

    // Verifies that zero and negative lengths are ignored.
    [Fact]
    public void BuildNonPositiveLengthsAreIgnored()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(runtime: 0), 5f),
            new WeightedMediaInteraction(CreateMedia(runtime: -100), 5f),
            new WeightedMediaInteraction(CreateMedia(runtime: 150), 1f));

        Assert.Equal(150f, profile.PreferredRuntimeMinutes);
    }

    // Input validation

    // Verifies that a null interaction collection is rejected.
    [Fact]
    public void BuildNullInteractionsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), null!));
    }

    // Verifies that null interactions within a collection are rejected.
    [Fact]
    public void BuildNullInteractionThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), [null!]));
    }

    // Verifies that interactions containing null media are rejected.
    [Fact]
    public void BuildNullMediaThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            BuildProfile(new WeightedMediaInteraction(null!, 1f)));
    }

    // Verifies that negative and nonfinite interaction weights are rejected.
    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void BuildInvalidWeightThrowsArgumentException(float weight)
    {
        var media = CreateMedia();

        Assert.Throws<ArgumentException>(() =>
            BuildProfile(new WeightedMediaInteraction(media, weight)));
    }

    // Verifies that overflowing accumulated preference weights are rejected.
    [Fact]
    public void BuildAccumulatedWeightOverflowThrowsException()
    {
        var media = CreateMedia(genres: ["Action"]);

        Assert.Throws<OverflowException>(() => BuildProfile(
            new WeightedMediaInteraction(media, float.MaxValue),
            new WeightedMediaInteraction(media, float.MaxValue)));
    }

    // Algorithm properties

    // Verifies that separate users maintain independent preference profiles.
    [Fact]
    public void BuildSeparateUsersProducesIndependentProfiles()
    {
        var first = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: ["Action"]), 3f));
        var second = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: ["Romance"]), 2f));

        Assert.True(first.GenrePreferences.ContainsKey("Action"));
        Assert.False(first.GenrePreferences.ContainsKey("Romance"));
        Assert.True(second.GenrePreferences.ContainsKey("Romance"));
        Assert.False(second.GenrePreferences.ContainsKey("Action"));
    }

    // Verifies that interaction ordering does not influence preferences.
    [Fact]
    public void BuildReorderedInteractionsReturnsSamePreferences()
    {
        var first = new WeightedMediaInteraction(
            CreateMedia(genres: ["Action"], runtime: 100), 3f);
        var second = new WeightedMediaInteraction(
            CreateMedia(genres: ["Action", "Drama"], runtime: 200), 2f);

        Guid userId = Guid.NewGuid();

        var original = UserProfileBuilder.Build(userId, [first, second]);
        var reordered = UserProfileBuilder.Build(userId, [second, first]);

        Assert.Equal(original.GenrePreferences.Count, reordered.GenrePreferences.Count);

        foreach (var preference in original.GenrePreferences)
            Assert.Equal(preference.Value, reordered.GenrePreferences[preference.Key], 6);

        Assert.Equal(original.PreferredRuntimeMinutes, reordered.PreferredRuntimeMinutes);
    }

    // Verifies that proportionally scaling weights preserves preferred length.
    [Fact]
    public void BuildScaledWeightsPreservesPreferredLength()
    {
        var first = CreateMedia(runtime: 100);
        var second = CreateMedia(runtime: 200);

        var original = BuildProfile(
            new WeightedMediaInteraction(first, 3f),
            new WeightedMediaInteraction(second, 1f));

        var scaled = BuildProfile(
            new WeightedMediaInteraction(first, 30f),
            new WeightedMediaInteraction(second, 10f));

        Assert.Equal(original.PreferredRuntimeMinutes, scaled.PreferredRuntimeMinutes);
    }

    // Verifies that building a profile does not modify the input media features.
    [Fact]
    public void BuildDoesNotModifyInputFeatures()
    {
        string[] genres = [" ACTION ", "Drama"];
        string[] original = (string[])genres.Clone();

        BuildProfile(new WeightedMediaInteraction(CreateMedia(genres: genres), 3f));

        Assert.Equal(original, genres);
    }

    // Verifies that every preference dictionary is read-only.
    [Fact]
    public void BuildReturnsReadOnlyPreferenceDictionaries()
    {
        var profile = BuildProfile(
            new WeightedMediaInteraction(CreateMedia(genres: ["Action"]), 3f));

        var dictionaries = new[]
        {
            profile.GenrePreferences,
            profile.AuthorPreferences,
            profile.DirectorPreferences,
            profile.StudioPreferences,
            profile.PublisherPreferences
        };

        foreach (var preferences in dictionaries)
        {
            var dictionary = Assert.IsAssignableFrom<IDictionary<string, float>>(preferences);

            Assert.Throws<NotSupportedException>(() => dictionary.Add("New Feature", 5f));
        }
    }
}