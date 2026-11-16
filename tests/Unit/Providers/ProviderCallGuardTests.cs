using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Backend.Providers;
using Backend.Shared.Errors;
using Xunit;

namespace Palette.Tests.Unit.Providers;

public class ProviderCallGuardTests
{
    private const string ProviderName = "test-provider";

    [Fact]
    public async Task SuccessfulCallReturnsOriginalResult()
    {
        var expected = new object();

        var result = await ProviderCallGuard.RunAsync(
            ProviderName,
            () => Task.FromResult(expected),
            CancellationToken.None);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task SuccessfulCallCanReturnNull()
    {
        // A lookup client can return null when the item does not exist.
        var result = await ProviderCallGuard.RunAsync<string?>(
            ProviderName,
            () => Task.FromResult<string?>(null),
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task HttpFailureBecomesProviderUnavailable()
    {
        var original = new HttpRequestException(
            "The upstream request failed.");

        var actual = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => Task.FromException<string>(original),
                CancellationToken.None));

        Assert.Equal(ProviderName, actual.ProviderName);
        Assert.Same(original, actual.InnerException);
    }

    [Fact]
    public async Task InvalidJsonBecomesProviderUnavailable()
    {
        var original = new JsonException(
            "The upstream response contains invalid JSON.");

        var actual = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => Task.FromException<string>(original),
                CancellationToken.None));

        Assert.Equal(ProviderName, actual.ProviderName);
        Assert.Same(original, actual.InnerException);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpstreamCancellationWithoutCallerCancellationBecomesProviderUnavailable(
        bool useTaskCanceledException)
    {
        // HttpClient timeouts can surface as either cancellation exception type.
        // The caller's token has not been cancelled.
        using var callerCancellation = new CancellationTokenSource();

        OperationCanceledException original = useTaskCanceledException
            ? new TaskCanceledException("The upstream request timed out.")
            : new OperationCanceledException("The upstream request timed out.");

        var actual = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => Task.FromException<string>(original),
                callerCancellation.Token));

        Assert.False(callerCancellation.IsCancellationRequested);
        Assert.Equal(ProviderName, actual.ProviderName);
        Assert.Same(original, actual.InnerException);
    }

    [Fact]
    public async Task CallerCancellationIsNotConvertedToProviderFailure()
    {
        using var callerCancellation = new CancellationTokenSource();
        callerCancellation.Cancel();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => Task.FromCanceled<string>(callerCancellation.Token),
                callerCancellation.Token));

        Assert.Equal(
            callerCancellation.Token,
            actual.CancellationToken);
    }

    [Fact]
    public async Task UnexpectedProgrammingErrorIsRethrownUnchanged()
    {
        var original = new InvalidOperationException(
            "An unexpected programming error.");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => Task.FromException<string>(original),
                CancellationToken.None));

        Assert.Same(original, actual);
    }

    [Fact]
    public async Task ExistingDomainErrorIsRethrownUnchanged()
    {
        var original = new MediaNotFoundException("movie", "42");

        var actual = await Assert.ThrowsAsync<MediaNotFoundException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => Task.FromException<string>(original),
                CancellationToken.None));

        Assert.Same(original, actual);
    }

    [Fact]
    public async Task SynchronouslyThrownHttpFailureIsAlsoConverted()
    {
        var original = new HttpRequestException(
            "The request failed before returning a task.");

        var actual = await Assert.ThrowsAsync<ProviderUnavailableException>(
            () => ProviderCallGuard.RunAsync<string>(
                ProviderName,
                () => throw original,
                CancellationToken.None));

        Assert.Equal(ProviderName, actual.ProviderName);
        Assert.Same(original, actual.InnerException);
    }
}