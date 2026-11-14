using Backend.Features.Lookup;
using Backend.Providers;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Palette.Tests.TestDoubles;

namespace Palette.Tests.Unit.Lookup;

public class LookupServiceTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public async Task MissingIdIsRejectedBeforeCallingProvider(string externalId)
    {
        var provider = new StubMediaProvider();

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            CreateService(provider).LookupAsync("movie", externalId, CancellationToken.None));

        Assert.Empty(provider.LookupCalls);
    }

    [Fact]
    public async Task LookupDispatchesToSelectedProviderWithTrimmedIdAndCancellationToken()
    {
        MediaLookupResult expected = new MovieLookupResult("42", "Example", null, null, null, 120);
        var selected = new StubMediaProvider
        {
            LookupHandler = (_, _) => Task.FromResult(expected),
        };
        var other = new StubMediaProvider("other");
        using var cancellation = new CancellationTokenSource();

        var actual = await CreateService(selected, other)
            .LookupAsync(" MOVIE ", " 42 ", cancellation.Token);

        var call = Assert.Single(selected.LookupCalls);
        Assert.Equal("42", call.ExternalId);
        Assert.Equal(cancellation.Token, call.CancellationToken);
        Assert.Same(expected, actual);
        Assert.Empty(other.LookupCalls);
    }

    [Fact]
    public async Task UnknownMediaTypeIsRejectedWithoutCallingRegisteredProvider()
    {
        var provider = new StubMediaProvider();

        await Assert.ThrowsAsync<UnknownMediaTypeException>(() =>
            CreateService(provider).LookupAsync("unregistered", "42", CancellationToken.None));

        Assert.Empty(provider.LookupCalls);
    }

    [Fact]
    public async Task MissingMediaFailureIsPreservedForTheApiErrorHandler()
    {
        var failure = new MediaNotFoundException("movie", "42");
        var provider = new StubMediaProvider
        {
            LookupHandler = (_, _) => Task.FromException<MediaLookupResult>(failure),
        };

        var actual = await Assert.ThrowsAsync<MediaNotFoundException>(() =>
            CreateService(provider).LookupAsync("movie", "42", CancellationToken.None));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task ProviderFailureIsPreservedForTheApiErrorHandler()
    {
        var failure = new ProviderUnavailableException("test-provider", new HttpRequestException());
        var provider = new StubMediaProvider
        {
            LookupHandler = (_, _) => Task.FromException<MediaLookupResult>(failure),
        };

        var actual = await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            CreateService(provider).LookupAsync("movie", "42", CancellationToken.None));

        Assert.Same(failure, actual);
    }

    private static LookupService CreateService(params IMediaProvider[] providers) =>
        new(new MediaProviderResolver(providers));
}
