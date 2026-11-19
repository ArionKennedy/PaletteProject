
using System;
using System.Collections.Generic;
using Palette.Api.MachineLearning.ContentBased;
using Xunit;

public class UserProfileBuilderTests
{
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
    // Profile Initialization Tests
    // ==================================================

    [Fact]
    // Verifies that building a profile preserves user id.
    public void BuildPreservesUserId()
    {
        Guid userId = Guid.NewGuid();

        var profile = UserProfileBuilder.Build(
            userId, Array.Empty<WeightedMediaInteraction>());

        Assert.Equal(userId, profile.UserId);
    }

    [Fact]
    // Verifies that building a profile with empty interactions returns empty profile.
    public void BuildEmptyInteractionsReturnsEmptyProfile()
    {
        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(),
            Array.Empty<WeightedMediaInteraction>());

        Assert.Empty(profile.GenrePreferences);
        Assert.Empty(profile.AuthorPreferences);
        Assert.Empty(profile.DirectorPreferences);
        Assert.Empty(profile.StudioPreferences);
        Assert.Empty(profile.PublisherPreferences);

        Assert.Null(profile.PreferredRuntimeMinutes);
        Assert.Null(profile.PreferredPageCount);
        Assert.Null(profile.PreferredNumberOfSeasons);
    }

    [Fact]
    // Verifies that building a profile with zero weight interaction does not affect profile.
    public void BuildZeroWeightInteractionDoesNotAffectProfile()
    {
        var media = CreateMedia(
            genres: new[] { "Action" },
            directors: new[] { "Christopher Nolan" },
            runtime: 120);

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 0f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Empty(profile.GenrePreferences);
        Assert.Empty(profile.DirectorPreferences);
        Assert.Null(profile.PreferredRuntimeMinutes);
    }

    // ==================================================
    // Categorical Preference Accumulation Tests
    // ==================================================

    [Fact]
    // Verifies building a profile for single interaction accumulates genre weight.
    public void BuildSingleInteractionAccumulatesGenreWeight()
    {
        var media = CreateMedia(
            genres: new[] { "Action" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(3f, profile.GenrePreferences["Action"]);
    }

    [Fact]
    // Verifies building a profile for multiple interactions accumulates genre weights.
    public void BuildMultipleInteractionsAccumulatesGenreWeights()
    {
        var first = CreateMedia(
            genres: new[] { "Action", "Adventure" });

        var second = CreateMedia(
            genres: new[] { "Action", "Drama" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 3f),
            new WeightedMediaInteraction(second, 2f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(5f, profile.GenrePreferences["Action"]);
        Assert.Equal(3f, profile.GenrePreferences["Adventure"]);
        Assert.Equal(2f, profile.GenrePreferences["Drama"]);
    }

    [Fact]
    // Verifies building a profile for accumulates author preferences.
    public void BuildAccumulatesAuthorPreferences()
    {
        var media = CreateMedia(
            mediaType: "book",
            authors: new[] { "J.R.R. Tolkien" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 4f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(
            4f, profile.AuthorPreferences["J.R.R. Tolkien"]);
    }

    [Fact]
    // Verifies building a profile for accumulates director preferences.
    public void BuildAccumulatesDirectorPreferences()
    {
        var media = CreateMedia(
            directors: new[] { "Christopher Nolan" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 5f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(
            5f, profile.DirectorPreferences["Christopher Nolan"]);
    }

    [Fact]
    // Verifies building a profile for accumulates studio preferences.
    public void BuildAccumulatesStudioPreferences()
    {
        var media = CreateMedia(
            studios: new[] { "Warner Bros." });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(
            3f, profile.StudioPreferences["Warner Bros."]);
    }

    [Fact]
    // Verifies building a profile for accumulates publisher preferences.
    public void BuildAccumulatesPublisherPreferences()
    {
        var media = CreateMedia(
            mediaType: "book",
            publishers: new[] { "HarperCollins" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 2f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(
            2f, profile.PublisherPreferences["HarperCollins"]);
    }

    [Fact]
    // Verifies building a profile for repeated media interactions accumulates weights.
    public void BuildRepeatedMediaInteractionsAccumulatesWeights()
    {
        var media = CreateMedia(
            genres: new[] { "Action" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 2f),
            new WeightedMediaInteraction(media, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(5f, profile.GenrePreferences["Action"]);
    }

    // ==================================================
    // Feature Normalization Tests
    // ==================================================

    [Fact]
    // Verifies that building a profile with different capitalization combines preferences.
    public void BuildDifferentCapitalizationCombinesPreferences()
    {
        var first = CreateMedia(
            genres: new[] { "ACTION" });

        var second = CreateMedia(
            genres: new[] { "action" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 2f),
            new WeightedMediaInteraction(second, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(5f, profile.GenrePreferences["Action"]);
    }

    [Fact]
    // Verifies that building a profile with extra whitespace combines preferences.
    public void BuildExtraWhitespaceCombinesPreferences()
    {
        var first = CreateMedia(
            genres: new[] { " Action " });

        var second = CreateMedia(
            genres: new[] { "Action" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 2f),
            new WeightedMediaInteraction(second, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(5f, profile.GenrePreferences["Action"]);
    }

    [Fact]
    // Verifies that building a profile with duplicate features does not inflate preferences.
    public void BuildDuplicateFeaturesDoesNotInflatePreferences()
    {
        var media = CreateMedia(
            genres: new[] { "Action", "ACTION", " Action " });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 4f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(4f, profile.GenrePreferences["Action"]);
    }

    [Fact]
    // Verifies building a profile for blank features are ignored.
    public void BuildBlankFeaturesAreIgnored()
    {
        var media = CreateMedia(
            genres: new[] { "", " ", "\t", "Action" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 2f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Single(profile.GenrePreferences);
        Assert.Equal(2f, profile.GenrePreferences["Action"]);
    }

    // ==================================================
    // Weighted Length Calculation Tests
    // ==================================================

    [Fact]
    // Verifies that building a profile with single movie returns its runtime.
    public void BuildSingleMovieReturnsItsRuntime()
    {
        var media = CreateMedia(runtime: 120);

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(120f, profile.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies that building a profile with equal weights returns the arithmetic mean.
    public void BuildEqualWeightsReturnsArithmeticMean()
    {
        var first = CreateMedia(runtime: 100);
        var second = CreateMedia(runtime: 200);

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 1f),
            new WeightedMediaInteraction(second, 1f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(150f, profile.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies that building a profile with different weights returns weighted mean.
    public void BuildDifferentWeightsReturnsWeightedMean()
    {
        var first = CreateMedia(runtime: 100);
        var second = CreateMedia(runtime: 200);

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 3f),
            new WeightedMediaInteraction(second, 1f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(125f, profile.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies that building a profile with books returns weighted page count.
    public void BuildBooksReturnsWeightedPageCount()
    {
        var first = CreateMedia(
            mediaType: "book", pageCount: 200);

        var second = CreateMedia(
            mediaType: "book", pageCount: 400);

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 1f),
            new WeightedMediaInteraction(second, 3f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(350f, profile.PreferredPageCount);
    }

    [Fact]
    // Verifies that building a profile with tv shows returns weighted season count.
    public void BuildTvShowsReturnsWeightedSeasonCount()
    {
        var first = CreateMedia(
            mediaType: "tv", seasons: 2);

        var second = CreateMedia(
            mediaType: "tv", seasons: 6);

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 3f),
            new WeightedMediaInteraction(second, 1f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(3f, profile.PreferredNumberOfSeasons);
    }

    [Fact]
    // Verifies that building a profile with mixed media keeps length preferences separate.
    public void BuildMixedMediaKeepsLengthPreferencesSeparate()
    {
        var movie = CreateMedia(runtime: 150);

        var book = CreateMedia(
            mediaType: "book", pageCount: 350);

        var tv = CreateMedia(
            mediaType: "tv", seasons: 5);

        var interactions = new[]
        {
            new WeightedMediaInteraction(movie, 3f),
            new WeightedMediaInteraction(book, 2f),
            new WeightedMediaInteraction(tv, 4f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(150f, profile.PreferredRuntimeMinutes);
        Assert.Equal(350f, profile.PreferredPageCount);
        Assert.Equal(5f, profile.PreferredNumberOfSeasons);
    }

    [Fact]
    // Verifies that building a profile with missing lengths does not affect weighted mean.
    public void BuildMissingLengthsDoesNotAffectWeightedMean()
    {
        var first = CreateMedia(runtime: 120);
        var second = CreateMedia(runtime: null);

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 1f),
            new WeightedMediaInteraction(second, 5f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(120f, profile.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies building a profile for non positive lengths are ignored.
    public void BuildNonPositiveLengthsAreIgnored()
    {
        var first = CreateMedia(runtime: 0);
        var second = CreateMedia(runtime: -100);
        var third = CreateMedia(runtime: 150);

        var interactions = new[]
        {
            new WeightedMediaInteraction(first, 5f),
            new WeightedMediaInteraction(second, 5f),
            new WeightedMediaInteraction(third, 1f)
        };

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(), interactions);

        Assert.Equal(150f, profile.PreferredRuntimeMinutes);
    }

    // ==================================================
    // Invalid Input Tests
    // ==================================================

    [Fact]
    // Verifies that building a profile with null interactions throws ArgumentNullException.
    public void BuildNullInteractionsThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), null!));
    }

    [Fact]
    // Verifies that building a profile with null interaction throws ArgumentNullException.
    public void BuildNullInteractionThrowsArgumentNullException()
    {
        var interactions = new WeightedMediaInteraction[]
        {
            null!
        };

        Assert.Throws<ArgumentNullException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), interactions));
    }

    [Fact]
    // Verifies that building a profile with null media throws ArgumentNullException.
    public void BuildNullMediaThrowsArgumentNullException()
    {
        var interactions = new[]
        {
            new WeightedMediaInteraction(null!, 1f)
        };

        Assert.Throws<ArgumentNullException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), interactions));
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(-0.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    // Verifies that building a profile with invalid weight throws ArgumentException.
    public void BuildInvalidWeightThrowsArgumentException(
        float weight)
    {
        var media = CreateMedia();

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, weight)
        };

        Assert.Throws<ArgumentException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), interactions));
    }

    [Fact]
    // Verifies that building a profile with accumulated weight overflow throws an exception.
    public void BuildAccumulatedWeightOverflowThrowsException()
    {
        var media = CreateMedia(
            genres: new[] { "Action" });

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, float.MaxValue),
            new WeightedMediaInteraction(media, float.MaxValue)
        };

        Assert.Throws<OverflowException>(() =>
            UserProfileBuilder.Build(Guid.NewGuid(), interactions));
    }

    // ==================================================
    // Algorithm Property Tests
    // ==================================================

    [Fact]
    // Verifies that building a profile with separate users produces independent profiles.
    public void BuildSeparateUsersProducesIndependentProfiles()
    {
        var firstMedia = CreateMedia(
            genres: new[] { "Action" });

        var secondMedia = CreateMedia(
            genres: new[] { "Romance" });

        var first = UserProfileBuilder.Build(
            Guid.NewGuid(),
            new[] { new WeightedMediaInteraction(firstMedia, 3f) });

        var second = UserProfileBuilder.Build(
            Guid.NewGuid(),
            new[] { new WeightedMediaInteraction(secondMedia, 2f) });

        Assert.True(first.GenrePreferences.ContainsKey("Action"));
        Assert.False(first.GenrePreferences.ContainsKey("Romance"));

        Assert.True(second.GenrePreferences.ContainsKey("Romance"));
        Assert.False(second.GenrePreferences.ContainsKey("Action"));
    }

    [Fact]
    // Verifies that building a profile with reordered interactions returns the same preferences.
    public void BuildReorderedInteractionsReturnsSamePreferences()
    {
        var first = new WeightedMediaInteraction(
            CreateMedia(
                genres: new[] { "Action" },
                runtime: 100), 3f);

        var second = new WeightedMediaInteraction(
            CreateMedia(
                genres: new[] { "Action", "Drama" },
                runtime: 200), 2f);

        Guid userId = Guid.NewGuid();

        var original = UserProfileBuilder.Build(
            userId, new[] { first, second });

        var reordered = UserProfileBuilder.Build(
            userId, new[] { second, first });

        Assert.Equal(
            original.GenrePreferences.Count,
            reordered.GenrePreferences.Count);

        foreach (var preference in original.GenrePreferences)
        {
            Assert.Equal(
                preference.Value,
                reordered.GenrePreferences[preference.Key], 6);
        }

        Assert.Equal(
            original.PreferredRuntimeMinutes,
            reordered.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies that building a profile with scaled weights preserves preferred length.
    public void BuildScaledWeightsPreservesPreferredLength()
    {
        var first = CreateMedia(runtime: 100);
        var second = CreateMedia(runtime: 200);

        var original = UserProfileBuilder.Build(
            Guid.NewGuid(),
            new[]
            {
                new WeightedMediaInteraction(first, 3f),
                new WeightedMediaInteraction(second, 1f)
            });

        var scaled = UserProfileBuilder.Build(
            Guid.NewGuid(),
            new[]
            {
                new WeightedMediaInteraction(first, 30f),
                new WeightedMediaInteraction(second, 10f)
            });

        Assert.Equal(
            original.PreferredRuntimeMinutes,
            scaled.PreferredRuntimeMinutes);
    }

    [Fact]
    // Verifies that building a profile does not modify input features.
    public void BuildDoesNotModifyInputFeatures()
    {
        string[] genres = { " ACTION ", "Drama" };
        string[] original = (string[])genres.Clone();

        var media = CreateMedia(genres: genres);

        var interactions = new[]
        {
            new WeightedMediaInteraction(media, 3f)
        };

        UserProfileBuilder.Build(Guid.NewGuid(), interactions);

        Assert.Equal(original, genres);
    }

    [Fact]
    // Verifies that building a profile returns read only preference dictionaries.
    public void BuildReturnsReadOnlyPreferenceDictionaries()
    {
        var media = CreateMedia(
            genres: new[] { "Action" });

        var profile = UserProfileBuilder.Build(
            Guid.NewGuid(),
            new[] { new WeightedMediaInteraction(media, 3f) });

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
            var dictionary =
                Assert.IsAssignableFrom<IDictionary<string, float>>(
                    preferences);

            Assert.Throws<NotSupportedException>(() =>
                dictionary.Add("New Feature", 5f));
        }
    }
}
