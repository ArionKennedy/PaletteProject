using System.Text.Json;
using Backend.Shared.Errors;

namespace Backend.Providers;

public static class ProviderCallGuard
{
    public static async Task<T> RunAsync<T>(
        string providerName,
        Func<Task<T>> call,
        CancellationToken cancellationToken)
    {
        try
        {
            return await call();
        }
        catch (Exception exception) when (IsUpstreamFailure(exception, cancellationToken))
        {
            throw new ProviderUnavailableException(providerName, exception);
        }
    }

    private static bool IsUpstreamFailure(
        Exception exception,
        CancellationToken cancellationToken) =>
        exception switch
        {
            HttpRequestException => true, // network error or a non-success status code
            JsonException => true,        // the response body was not what we expected
            OperationCanceledException => !cancellationToken.IsCancellationRequested,  //if cancelled by user.

            _ => false,
        };
}
