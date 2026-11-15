
using Palette.Api.MachineLearning;

namespace Palette.Tests.MachineLearning;

public class SyntheticDataGeneratorTests
{
    [Fact]
    public void GenerateExpectedNumberOfRatings()
    {
        // Arrange
        int userCount = 100;
        int movieCount = 200;
        int ratingsPerUser = 30;

        // Act
        var ratings = DataGenerator.Generate(
            userCount,
            movieCount,
            ratingsPerUser
        );

        // Assert
        Assert.Equal(3000, ratings.Count);

        Assert.All(ratings, rating =>
        {
            Assert.InRange(rating.Rating, 1f, 5f);
        });
    }
}
