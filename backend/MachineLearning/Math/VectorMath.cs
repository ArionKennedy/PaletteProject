namespace Palette.Api.MachineLearning.LinearAlgebra;

public static class VectorMath
{
    public static float DotProduct(
        ReadOnlySpan<float> factorA,
        ReadOnlySpan<float> factorB)
    {
        if (factorA.Length != factorB.Length)
        {
            throw new ArgumentException(
                "Vectors must have the same length.");
        }

        float result = 0f;

        for (int i = 0; i < factorA.Length; i++)
        {
            result += factorA[i] * factorB[i];
        }

        return result;
    }

    public static float Magnitude(
        ReadOnlySpan<float> vector)
    {
        return MathF.Sqrt(DotProduct(vector, vector));
    }

    public static float CosineSimilarity(
        ReadOnlySpan<float> vectorA,
        ReadOnlySpan<float> vectorB)
    {
        if (vectorA.Length != vectorB.Length)
        {
            throw new ArgumentException(
                "Vectors must have the same length.");
        }

        float magnitudeA = Magnitude(vectorA);
        float magnitudeB = Magnitude(vectorB);

        if (magnitudeA == 0f || magnitudeB == 0f)
        {
            return 0f;
        }

        float dotProduct = DotProduct(vectorA, vectorB);

        float similarity =
            dotProduct / (magnitudeA * magnitudeB);

        return Math.Clamp(similarity, -1f, 1f);
    }
}