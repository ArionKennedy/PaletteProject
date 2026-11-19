using System;
using Palette.Api.MachineLearning.LinearAlgebra;
using Xunit;

public class VectorMathTests
{
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
    public void DotProductUnequalLengthsThrowsArgumentException(
        float[] a,
        float[] b)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            VectorMath.DotProduct(a, b));
    }

    [Fact]
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
}