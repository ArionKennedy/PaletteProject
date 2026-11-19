
using System;
using Palette.Api.MachineLearning.LinearAlgebra;
using Xunit;

public class VectorMathTests
{
    // Dot Product Tests
    [Theory]
    [InlineData(new float[] { 1, 2, 3 },
                new float[] { 4, 5, 6 }, 32f)]
    [InlineData(new float[] { 0, 0, 0 },
                new float[] { 1, 2, 3 }, 0f)]
    [InlineData(new float[] { -1, -2 },
                new float[] { 3, 4 }, -11f)]
    [InlineData(new float[] { -1, 2 },
                new float[] { 3, 4 }, 5f)]
    [InlineData(new float[] { -1, -2 },
                new float[] { -3, -4 }, 11f)]
    [InlineData(new float[] { 1, 0 },
                new float[] { 0, 1 }, 0f)]
    [InlineData(new float[] { 3 },
                new float[] { 4 }, 12f)]
    [InlineData(new float[] { },
                new float[] { }, 0f)]
    [InlineData(new float[] { 0.5f, 1.5f },
                new float[] { 2, 3 }, 5.5f)]
    // Verifies that calculating a dot product with valid vectors returns the expected result.
    public void DotProductValidVectorsReturnsExpectedResult(
        float[] a,
        float[] b,
        float expected)
    {
        // Act
        float result = VectorMath.DotProduct(a, b);

        // Assert
        Assert.Equal(expected, result, 6);
    }

    [Theory]
    [InlineData(new float[] { 1, 2 }, new float[] { 3 })]
    [InlineData(new float[] { 1 }, new float[] { 2, 3 })]
    [InlineData(new float[] { }, new float[] { 1 })]
    [InlineData(new float[] { 1 }, new float[] { })]
    // Verifies that calculating a dot product with unequal lengths throws ArgumentException.
    public void DotProductUnequalLengthsThrowsArgumentException(
        float[] a,
        float[] b)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            VectorMath.DotProduct(a, b));
    }

    [Fact]
    // Verifies that calculating a dot product with reversed vectors returns the same result.
    public void DotProductReversedVectorsReturnsSameResult()
    {
        // Arrange
        float[] a = { 1f, 2f, 3f };
        float[] b = { 4f, 5f, 6f };

        // Act
        float first = VectorMath.DotProduct(a, b);
        float second = VectorMath.DotProduct(b, a);

        // Assert
        Assert.Equal(first, second);
    }

    [Fact]
    // Verifies that calculating a dot product with valid vectors does not modify inputs.
    public void DotProductValidVectorsDoesNotModifyInputs()
    {
        // Arrange
        float[] a = { 1f, 2f, 3f };
        float[] b = { 4f, 5f, 6f };

        float[] originalA = (float[])a.Clone();
        float[] originalB = (float[])b.Clone();

        // Act
        VectorMath.DotProduct(a, b);

        // Assert
        Assert.Equal(originalA, a);
        Assert.Equal(originalB, b);
    }

    // Magnitude Tests

    [Theory]
    [InlineData(new float[] { 3, 4 }, 5f)]
    [InlineData(new float[] { -3, -4 }, 5f)]
    [InlineData(new float[] { 0, 0 }, 0f)]
    [InlineData(new float[] { 1, 2, 2 }, 3f)]
    [InlineData(new float[] { 5 }, 5f)]
    [InlineData(new float[] { -5 }, 5f)]
    [InlineData(new float[] { }, 0f)]
    [InlineData(new float[] { 0.3f, 0.4f }, 0.5f)]
    // Verifies that calculating vector magnitude with valid vector returns the expected result.
    public void MagnitudeValidVectorReturnsExpectedResult(
        float[] vector,
        float expected)
    {
        // Act
        float result = VectorMath.Magnitude(vector);

        // Assert
        Assert.Equal(expected, result, 6);
    }

    [Fact]
    // Verifies that calculating vector magnitude with scaled vector returns scaled magnitude.
    public void MagnitudeScaledVectorReturnsScaledMagnitude()
    {
        // Arrange
        float[] a = { 3f, 4f };
        float[] scaledA = { 9f, 12f };

        // Act
        float original = VectorMath.Magnitude(a);
        float scaled = VectorMath.Magnitude(scaledA);

        // Assert
        Assert.Equal(original * 3f, scaled, 6);
    }

    [Fact]
    // Verifies that calculating vector magnitude does not modify input.
    public void MagnitudeDoesNotModifyInput()
    {
        // Arrange
        float[] a = { 1f, 2f, 3f };
        float[] original = (float[])a.Clone();

        // Act
        VectorMath.Magnitude(a);

        // Assert
        Assert.Equal(original, a);
    }

    // Cosine Similarity Tests

    [Theory]
    [InlineData(new float[] { 1, 2, 3 },
                new float[] { 1, 2, 3 }, 1f)]
    [InlineData(new float[] { 1, 0 },
                new float[] { 0, 1 }, 0f)]
    [InlineData(new float[] { 1, 2 },
                new float[] { -1, -2 }, -1f)]
    [InlineData(new float[] { 1, 2 },
                new float[] { 2, 4 }, 1f)]
    [InlineData(new float[] { 0, 0 },
                new float[] { 1, 2 }, 0f)]
    [InlineData(new float[] { 1, 2 },
                new float[] { 0, 0 }, 0f)]
    [InlineData(new float[] { 0, 0 },
                new float[] { 0, 0 }, 0f)]
    [InlineData(new float[] { },
                new float[] { }, 0f)]
    [InlineData(new float[] { 1 },
                new float[] { 5 }, 1f)]
    [InlineData(new float[] { 1 },
                new float[] { -5 }, -1f)]
    [InlineData(new float[] { 1, 0 },
                new float[] { 1, 1 }, 0.707107f)]
    [InlineData(new float[] { 1, 0 },
                new float[] { -1, 1 }, -0.707107f)]
    [InlineData(new float[] { 1, 1, 0 },
                new float[] { 1, 0, 1 }, 0.5f)]
    // Verifies that calculating cosine similarity with valid vectors returns the expected result.
    public void CosineSimilarityValidVectorsReturnsExpectedResult(
        float[] a,
        float[] b,
        float expected)
    {
        // Act
        float result = VectorMath.CosineSimilarity(a, b);

        // Assert
        Assert.Equal(expected, result, 6);
    }

    [Theory]
    [InlineData(new float[] { 1, 2 }, new float[] { 1 })]
    [InlineData(new float[] { 1 }, new float[] { 1, 2 })]
    [InlineData(new float[] { }, new float[] { 1 })]
    [InlineData(new float[] { 1 }, new float[] { })]
    // Verifies that calculating cosine similarity with unequal lengths throws ArgumentException.
    public void CosineSimilarityUnequalLengthsThrowsArgumentException(
        float[] a,
        float[] b)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            VectorMath.CosineSimilarity(a, b));
    }

    [Fact]
    // Verifies that calculating cosine similarity with reversed vectors returns the same result.
    public void CosineSimilarityReversedVectorsReturnsSameResult()
    {
        // Arrange
        float[] a = { 1f, 2f, 3f };
        float[] b = { 4f, 5f, 6f };

        // Act
        float first = VectorMath.CosineSimilarity(a, b);
        float second = VectorMath.CosineSimilarity(b, a);

        // Assert
        Assert.Equal(first, second, 6);
    }

    [Fact]
    // Verifies that calculating cosine similarity with scaled vectors returns the same result.
    public void CosineSimilarityScaledVectorsReturnsSameResult()
    {
        // Arrange
        float[] a = { 1f, 2f, 3f };
        float[] b = { 4f, 5f, 6f };
        float[] scaledA = { 10f, 20f, 30f };

        // Act
        float original = VectorMath.CosineSimilarity(a, b);
        float scaled = VectorMath.CosineSimilarity(scaledA, b);

        // Assert
        Assert.Equal(original, scaled, 6);
    }

    [Fact]
    // Verifies that calculating cosine similarity with random vectors remains in range.
    public void CosineSimilarityRandomVectorsRemainsInRange()
    {
        // Arrange
        Random random = new Random(42);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            float[] a = new float[10];
            float[] b = new float[10];

            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (float)(random.NextDouble() * 2 - 1);
                b[i] = (float)(random.NextDouble() * 2 - 1);
            }

            // Act
            float result = VectorMath.CosineSimilarity(a, b);

            // Assert
            Assert.True(float.IsFinite(result));
            Assert.InRange(result, -1f, 1f);
        }
    }

    [Fact]
    // Verifies that calculating cosine similarity does not modify inputs.
    public void CosineSimilarityDoesNotModifyInputs()
    {
        // Arrange
        float[] a = { 1f, 2f, 3f };
        float[] b = { 4f, 5f, 6f };

        float[] originalA = (float[])a.Clone();
        float[] originalB = (float[])b.Clone();

        // Act
        VectorMath.CosineSimilarity(a, b);

        // Assert
        Assert.Equal(originalA, a);
        Assert.Equal(originalB, b);
    }
}
