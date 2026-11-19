
using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;
using Palette.Api.Enums;
using Palette.Api.MachineLearning.ContentBased;
using Palette.Api.MachineLearning.Hybrid;
using Backend.Features.Recommendations.Data;

namespace Palette.Tests;

public class ReviewStatisticsServiceTests : IDisposable
{
    private readonly PaletteDbContext _context;
    private readonly ReviewStatisticsService _service;

    // Initializes an isolated database and review statistics service.
    public ReviewStatisticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        _context = new PaletteDbContext(options);
        _service = new ReviewStatisticsService(_context);
    }

    // Releases the database context after each test.
    public void Dispose() => _context.Dispose();

    // Creates a media item with optional review ratings.
    private Guid CreateMedia(params int[] ratings)
    {
        Guid id = Guid.NewGuid();

        _context.Media.Add(new Media
        {
            MediaId = id,
            Title = $"Media-{id}",
            MediaType = MediaType.Movie
        });

        AddReviews(id, ratings);
        return id;
    }

    // Creates a unique user for a review.
    private Guid CreateUser()
    {
        Guid id = Guid.NewGuid();

        _context.Users.Add(new User
        {
            UserId = id,
            Username = $"User-{id}",
            Email = $"{id}@test.com",
            Auth0Id = $"auth0|{id}"
        });

        return id;
    }

    // Adds reviews with the specified ratings to a media item.
    private void AddReviews(Guid mediaId, params int[] ratings)
    {
        foreach (int rating in ratings)
        {
            _context.Reviews.Add(new Review
            {
                ReviewId = Guid.NewGuid(),
                UserId = CreateUser(),
                MediaId = mediaId,
                Rating = rating,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    // Saves pending database changes and retrieves review statistics.
    private async Task<CommunityReviewStatistics> GetStatistics()
    {
        await _context.SaveChangesAsync();
        return await _service.GetStatisticsAsync();
    }

    // Creates a recommendation candidate using database review statistics.
    private static RecommendationCandidate CreateCandidate(
        Guid id, CommunityReviewStatistics statistics)
    {
        var review = statistics.GetMediaStatistics(id);
        var media = new MediaFeatures(id, "movie", [], [], [], [], []);

        return new RecommendationCandidate(
            media, review.ReviewCount, review.AverageRating);
    }

    // Creates a cold-start recommendation context using community statistics.
    private static RecommendationContext CreateContext(
        CommunityReviewStatistics statistics)
    {
        var profile = UserProfileBuilder.Build(Guid.NewGuid(), []);

        return new RecommendationContext(profile, new HashSet<Guid>(),
            statistics.MaxReviewCount, statistics.GlobalAverageRating);
    }

    // Review aggregation

    // Verifies that all valid reviews are counted.
    [Fact]
    public async Task GetStatisticsReturnsCorrectReviewCount()
    {
        Guid id = CreateMedia(5, 4, 3);
        var result = await GetStatistics();

        Assert.Equal(3, result.GetMediaStatistics(id).ReviewCount);
    }

    // Verifies that the media average rating is calculated correctly.
    [Fact]
    public async Task GetStatisticsReturnsCorrectAverageRating()
    {
        Guid id = CreateMedia(5, 4, 3);
        var result = await GetStatistics();

        Assert.Equal(4f, result.GetMediaStatistics(id).AverageRating!.Value, 6);
    }

    // Verifies that reviews are grouped by their media identifiers.
    [Fact]
    public async Task GetStatisticsGroupsReviewsByMedia()
    {
        Guid first = CreateMedia(5, 4, 3);
        Guid second = CreateMedia(2, 4);
        var result = await GetStatistics();

        Assert.Equal(2, result.MediaStatistics.Count);
        Assert.Equal(new MediaReviewStatistics(first, 3, 4f),
            result.GetMediaStatistics(first));
        Assert.Equal(new MediaReviewStatistics(second, 2, 3f),
            result.GetMediaStatistics(second));
    }

    // Verifies that the global average accounts for every individual review.
    [Fact]
    public async Task GetStatisticsReturnsWeightedGlobalAverage()
    {
        CreateMedia(5, 5, 5, 5);
        CreateMedia(1);

        Assert.Equal(4.2f, (await GetStatistics()).GlobalAverageRating, 6);
    }

    // Verifies that the highest media review count is returned.
    [Fact]
    public async Task GetStatisticsReturnsMaximumReviewCount()
    {
        CreateMedia(5, 4);
        CreateMedia(3, 4, 5, 2, 1);
        CreateMedia(4);

        Assert.Equal(5, (await GetStatistics()).MaxReviewCount);
    }

    // Verifies that fractional average ratings retain their precision.
    [Fact]
    public async Task GetStatisticsSupportsFractionalRatings()
    {
        Guid id = CreateMedia(5, 4, 4);
        var result = await GetStatistics();

        Assert.Equal(13f / 3f, result.GetMediaStatistics(id).AverageRating!.Value, 6);
    }

    // Empty database and missing reviews

    // Verifies that an empty database returns neutral defaults.
    [Fact]
    public async Task GetStatisticsEmptyDatabaseReturnsDefaults()
    {
        var result = await GetStatistics();

        Assert.Empty(result.MediaStatistics);
        Assert.Equal(0, result.MaxReviewCount);
        Assert.Equal(3f, result.GlobalAverageRating);
    }

    // Verifies that unreviewed media are excluded from aggregation.
    [Fact]
    public async Task GetStatisticsMediaWithoutReviewsIsNotAggregated()
    {
        CreateMedia();

        Assert.Empty((await GetStatistics()).MediaStatistics);
    }

    // Verifies that unknown media receive default review statistics.
    [Fact]
    public async Task GetMediaStatisticsUnknownMediaReturnsDefaults()
    {
        Guid id = Guid.NewGuid();
        var result = await GetStatistics();

        Assert.Equal(new MediaReviewStatistics(id, 0, null),
            result.GetMediaStatistics(id));
    }

    // Verifies that unreviewed media do not influence community statistics.
    [Fact]
    public async Task GetStatisticsUnreviewedMediaDoesNotAffectGlobalAverage()
    {
        CreateMedia(5, 5);

        for (int i = 0; i < 10; i++)
            CreateMedia();

        var result = await GetStatistics();

        Assert.Single(result.MediaStatistics);
        Assert.Equal(5f, result.GlobalAverageRating);
        Assert.Equal(2, result.MaxReviewCount);
    }

    // Invalid rating handling

    // Verifies that ratings below one are ignored.
    [Fact]
    public async Task GetStatisticsIgnoresRatingsBelowOne()
    {
        Guid id = CreateMedia(-5, 0, 4);
        var result = await GetStatistics();

        Assert.Equal(new MediaReviewStatistics(id, 1, 4f),
            result.GetMediaStatistics(id));
    }

    // Verifies that ratings above five are ignored.
    [Fact]
    public async Task GetStatisticsIgnoresRatingsAboveFive()
    {
        Guid id = CreateMedia(6, 100, 3);
        var result = await GetStatistics();

        Assert.Equal(new MediaReviewStatistics(id, 1, 3f),
            result.GetMediaStatistics(id));
    }

    // Verifies that exclusively invalid ratings return neutral defaults.
    [Fact]
    public async Task GetStatisticsAllInvalidRatingsReturnsDefaults()
    {
        CreateMedia(-1, 0, 6, 100);
        var result = await GetStatistics();

        Assert.Empty(result.MediaStatistics);
        Assert.Equal(0, result.MaxReviewCount);
        Assert.Equal(3f, result.GlobalAverageRating);
    }

    // Verifies that invalid ratings do not influence global statistics.
    [Fact]
    public async Task GetStatisticsInvalidRatingsDoNotAffectGlobalStatistics()
    {
        CreateMedia(5, 3);
        CreateMedia(0, 6, -1, 100);
        var result = await GetStatistics();

        Assert.Single(result.MediaStatistics);
        Assert.Equal(2, result.MaxReviewCount);
        Assert.Equal(4f, result.GlobalAverageRating);
    }

    // Statistical properties

    // Verifies that one is accepted as the minimum valid rating.
    [Fact]
    public async Task GetStatisticsMinimumValidRatingIsOne()
    {
        Guid id = CreateMedia(1, 1, 1);
        var result = await GetStatistics();

        Assert.Equal(1f, result.GlobalAverageRating);
        Assert.Equal(1f, result.GetMediaStatistics(id).AverageRating!.Value);
    }

    // Verifies that five is accepted as the maximum valid rating.
    [Fact]
    public async Task GetStatisticsMaximumValidRatingIsFive()
    {
        Guid id = CreateMedia(5, 5, 5);
        var result = await GetStatistics();

        Assert.Equal(5f, result.GlobalAverageRating);
        Assert.Equal(5f, result.GetMediaStatistics(id).AverageRating!.Value);
    }

    // Verifies that statistics update when new reviews are added.
    [Fact]
    public async Task GetStatisticsReflectsNewReviews()
    {
        Guid id = CreateMedia(5);
        var before = await GetStatistics();

        AddReviews(id, 1);
        var after = await GetStatistics();

        Assert.Equal(5f, before.GlobalAverageRating);
        Assert.Equal(3f, after.GlobalAverageRating);
        Assert.Equal(1, before.MaxReviewCount);
        Assert.Equal(2, after.MaxReviewCount);
    }

    // Verifies that unchanged data produces consistent statistics.
    [Fact]
    public async Task GetStatisticsRepeatedCallsReturnConsistentValues()
    {
        Guid id = CreateMedia(5, 4, 2);
        var first = await GetStatistics();
        var second = await GetStatistics();

        Assert.Equal(first.GlobalAverageRating, second.GlobalAverageRating);
        Assert.Equal(first.MaxReviewCount, second.MaxReviewCount);
        Assert.Equal(first.GetMediaStatistics(id), second.GetMediaStatistics(id));
    }

    // Verifies aggregation accuracy across a randomized dataset.
    [Fact]
    public async Task GetStatisticsRandomDatasetProducesCorrectAggregates()
    {
        var random = new Random(42);
        var expected = new Dictionary<Guid, int[]>();

        for (int i = 0; i < 50; i++)
        {
            var ratings = Enumerable.Range(0, random.Next(1, 30))
                .Select(_ => random.Next(1, 6)).ToArray();

            expected.Add(CreateMedia(ratings), ratings);
        }

        var result = await GetStatistics();

        Assert.Equal(50, result.MediaStatistics.Count);

        foreach (var entry in expected)
        {
            var statistics = result.GetMediaStatistics(entry.Key);

            Assert.Equal(entry.Value.Length, statistics.ReviewCount);
            Assert.Equal((float)entry.Value.Average(),
                statistics.AverageRating!.Value, 6);
        }

        Assert.Equal(expected.Values.Max(r => r.Length), result.MaxReviewCount);
        Assert.Equal((float)expected.Values.SelectMany(r => r).Average(),
            result.GlobalAverageRating, 6);
    }

    // Service validation

    // Verifies that a null database context is rejected.
    [Fact]
    public void ConstructorNullContextThrowsException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ReviewStatisticsService(null!));
    }

    // Verifies that cancelled database requests are interrupted.
    [Fact]
    public async Task GetStatisticsCancelledRequestThrowsException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.GetStatisticsAsync(cancellation.Token));
    }

    // Hybrid engine integration

    // Verifies that database statistics produce the expected hybrid score.
    [Fact]
    public async Task DatabaseStatisticsProduceValidRecommendation()
    {
        Guid id = CreateMedia(5, 4, 3);
        var statistics = await GetStatistics();
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(CreateContext(statistics),
            CreateCandidate(id, statistics));

        Assert.Equal(id, result.MediaId);
        Assert.Equal(1f, result.PopularityScore);
        Assert.Equal(0.75f, result.RatingQualityScore, 6);
        Assert.Equal(5f / 6f, result.FinalScore, 6);
    }

    // Verifies that stronger community statistics improve recommendation ranking.
    [Fact]
    public async Task DatabaseStatisticsInfluenceRecommendationRanking()
    {
        Guid strong = CreateMedia(Enumerable.Repeat(5, 100).ToArray());
        Guid weak = CreateMedia(1, 2);
        var statistics = await GetStatistics();
        var engine = new HybridRecommendationEngine();

        var results = engine.Recommend(CreateContext(statistics),
            [CreateCandidate(weak, statistics), CreateCandidate(strong, statistics)]);

        Assert.Equal(2, results.Count);
        Assert.Equal(strong, results[0].MediaId);
        Assert.Equal(weak, results[1].MediaId);
        Assert.True(results[0].FinalScore > results[1].FinalScore);
    }

    // Verifies that recommendations can be scored without existing reviews.
    [Fact]
    public async Task EmptyDatabaseSupportsColdStartRecommendations()
    {
        var statistics = await GetStatistics();
        var engine = new HybridRecommendationEngine();

        var result = engine.Score(CreateContext(statistics),
            CreateCandidate(Guid.NewGuid(), statistics));

        Assert.Null(result.ContentScore);
        Assert.Equal(0f, result.PopularityScore);
        Assert.Equal(0.5f, result.RatingQualityScore);
        Assert.Equal(1f / 3f, result.FinalScore, 6);
    }
}
