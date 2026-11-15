namespace Palette.Api.MachineLearning.LinearAlgebra;
public static class VectorMath
{
    public static float DotProduct(ReadOnlySpan<float> factorA, ReadOnlySpan<float> factorB)
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
}
