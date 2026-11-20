
using System;
using Palette.Api.MachineLearning.LinearAlgebra;
using Xunit;

public class VectorMathTests
{
    // Dot product

    // Verifies that valid vectors produce the expected dot product.
    [Theory]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 4, 5, 6 }, 32f)]
    [InlineData(new float[] { 0, 0, 0 }, new float[] { 1, 2, 3 }, 0f)]
    [InlineData(new float[] { -1, -2 }, new float[] { 3, 4 }, -11f)]
    [InlineData(new float[] { -1, 2 }, new float[] { 3, 4 }, 5f)]
    [InlineData(new float[] { -1, -2 }, new float[] { -3, -4 }, 11f)]
    [InlineData(new float[] { 1, 0 }, new float[] { 0, 1 }, 0f)]
    [InlineData(new float[] { 3 }, new float[] { 4 }, 12f)]
    [InlineData(new float[] { }, new float[] { }, 0f)]
    [InlineData(new float[] { 0.5f, 1.5f }, new float[] { 2, 3 }, 5.5f)]
    public void DotProductValidVectorsReturnsExpectedResult(float[] a, float[] b, float expected)
    {
        Assert.Equal(expected, VectorMath.DotProduct(a, b), 6);
    }

    // Verifies that vectors of unequal lengths are rejected.
    [Theory]
    [InlineData(new float[] { 1, 2 }, new float[] { 3 })]
    [InlineData(new float[] { 1 }, new float[] { 2, 3 })]
    [InlineData(new float[] { }, new float[] { 1 })]
    [InlineData(new float[] { 1 }, new float[] { })]
    public void DotProductUnequalLengthsThrowsArgumentException(float[] a, float[] b)
    {
        Assert.Throws<ArgumentException>(() => VectorMath.DotProduct(a, b));
    }

    // Verifies that the dot product is commutative.
    [Fact]
    public void DotProductReversedVectorsReturnsSameResult()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [4f, 5f, 6f];

        Assert.Equal(VectorMath.DotProduct(a, b), VectorMath.DotProduct(b, a));
    }

    // Verifies that calculating the dot product does not modify either vector.
    [Fact]
    public void DotProductValidVectorsDoesNotModifyInputs()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [4f, 5f, 6f];
        float[] originalA = (float[])a.Clone();
        float[] originalB = (float[])b.Clone();

        VectorMath.DotProduct(a, b);

        Assert.Equal(originalA, a);
        Assert.Equal(originalB, b);
    }

    // Magnitude

    // Verifies that valid vectors produce the expected magnitude.
    [Theory]
    [InlineData(new float[] { 3, 4 }, 5f)]
    [InlineData(new float[] { -3, -4 }, 5f)]
    [InlineData(new float[] { 0, 0 }, 0f)]
    [InlineData(new float[] { 1, 2, 2 }, 3f)]
    [InlineData(new float[] { 5 }, 5f)]
    [InlineData(new float[] { -5 }, 5f)]
    [InlineData(new float[] { }, 0f)]
    [InlineData(new float[] { 0.3f, 0.4f }, 0.5f)]
    public void MagnitudeValidVectorReturnsExpectedResult(float[] vector, float expected)
    {
        Assert.Equal(expected, VectorMath.Magnitude(vector), 6);
    }

    // Verifies that scaling a vector proportionally scales its magnitude.
    [Fact]
    public void MagnitudeScaledVectorReturnsScaledMagnitude()
    {
        float original = VectorMath.Magnitude([3f, 4f]);
        float scaled = VectorMath.Magnitude([9f, 12f]);

        Assert.Equal(original * 3f, scaled, 6);
    }

    // Verifies that calculating magnitude does not modify the input vector.
    [Fact]
    public void MagnitudeDoesNotModifyInput()
    {
        float[] a = [1f, 2f, 3f];
        float[] original = (float[])a.Clone();

        VectorMath.Magnitude(a);

        Assert.Equal(original, a);
    }

    // Cosine similarity

    // Verifies that valid vectors produce the expected cosine similarity.
    [Theory]
    [InlineData(new float[] { 1, 2, 3 }, new float[] { 1, 2, 3 }, 1f)]
    [InlineData(new float[] { 1, 0 }, new float[] { 0, 1 }, 0f)]
    [InlineData(new float[] { 1, 2 }, new float[] { -1, -2 }, -1f)]
    [InlineData(new float[] { 1, 2 }, new float[] { 2, 4 }, 1f)]
    [InlineData(new float[] { 0, 0 }, new float[] { 1, 2 }, 0f)]
    [InlineData(new float[] { 1, 2 }, new float[] { 0, 0 }, 0f)]
    [InlineData(new float[] { 0, 0 }, new float[] { 0, 0 }, 0f)]
    [InlineData(new float[] { }, new float[] { }, 0f)]
    [InlineData(new float[] { 1 }, new float[] { 5 }, 1f)]
    [InlineData(new float[] { 1 }, new float[] { -5 }, -1f)]
    [InlineData(new float[] { 1, 0 }, new float[] { 1, 1 }, 0.707107f)]
    [InlineData(new float[] { 1, 0 }, new float[] { -1, 1 }, -0.707107f)]
    [InlineData(new float[] { 1, 1, 0 }, new float[] { 1, 0, 1 }, 0.5f)]
    public void CosineSimilarityValidVectorsReturnsExpectedResult(float[] a, float[] b, float expected)
    {
        Assert.Equal(expected, VectorMath.CosineSimilarity(a, b), 6);
    }

    // Verifies that cosine similarity rejects vectors of unequal lengths.
    [Theory]
    [InlineData(new float[] { 1, 2 }, new float[] { 1 })]
    [InlineData(new float[] { 1 }, new float[] { 1, 2 })]
    [InlineData(new float[] { }, new float[] { 1 })]
    [InlineData(new float[] { 1 }, new float[] { })]
    public void CosineSimilarityUnequalLengthsThrowsArgumentException(float[] a, float[] b)
    {
        Assert.Throws<ArgumentException>(() => VectorMath.CosineSimilarity(a, b));
    }

    // Verifies that cosine similarity is symmetric.
    [Fact]
    public void CosineSimilarityReversedVectorsReturnsSameResult()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [4f, 5f, 6f];

        Assert.Equal(VectorMath.CosineSimilarity(a, b),
            VectorMath.CosineSimilarity(b, a), 6);
    }

    // Verifies that positive vector scaling preserves cosine similarity.
    [Fact]
    public void CosineSimilarityScaledVectorsReturnsSameResult()
    {
        float[] b = [4f, 5f, 6f];
        float original = VectorMath.CosineSimilarity([1f, 2f, 3f], b);
        float scaled = VectorMath.CosineSimilarity([10f, 20f, 30f], b);

        Assert.Equal(original, scaled, 6);
    }

    // Verifies that 1,000 random vector pairs produce finite scores within [-1, 1].
    [Fact]
    public void CosineSimilarityRandomVectorsRemainsInRange()
    {
        var random = new Random(42);

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            float[] a = new float[10];
            float[] b = new float[10];

            for (int i = 0; i < a.Length; i++)
            {
                a[i] = (float)(random.NextDouble() * 2 - 1);
                b[i] = (float)(random.NextDouble() * 2 - 1);
            }

            float result = VectorMath.CosineSimilarity(a, b);

            Assert.True(float.IsFinite(result));
            Assert.InRange(result, -1f, 1f);
        }
    }

    // Verifies that cosine similarity does not modify either input vector.
    [Fact]
    public void CosineSimilarityDoesNotModifyInputs()
    {
        float[] a = [1f, 2f, 3f];
        float[] b = [4f, 5f, 6f];
        float[] originalA = (float[])a.Clone();
        float[] originalB = (float[])b.Clone();

        VectorMath.CosineSimilarity(a, b);

        Assert.Equal(originalA, a);
        Assert.Equal(originalB, b);
    }
}
