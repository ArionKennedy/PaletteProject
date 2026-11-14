using Backend.Features.Search;
using Backend.Providers;
using Backend.Shared.Errors;
using Backend.Shared.Models;
using Palette.Tests.TestDoubles;

namespace Palette.Tests.Unit.Search;

public class SearchServiceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public async Task MissingQueryIsRejectedBeforeCallingProvider(string? query)
    {
        var provider = new StubMediaProvider();
        var service = CreateService(provider);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            service.SearchAsync("movie", query, CancellationToken.None));

        Assert.Empty(provider.SearchCalls);
    }

    [Fact]
    public async Task QueryLongerThanTwoHundredCharactersIsRejectedBeforeCallingProvider()
    {
        var provider = new StubMediaProvider();
        var service = CreateService(provider);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            service.SearchAsync("movie", new string('x', 201), CancellationToken.None));

        Assert.Empty(provider.SearchCalls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(200)]
    public async Task ValidQueryLengthIsCheckedAfterTrimming(int length)
    {
        var provider = EmptyProvider();
        var service = CreateService(provider);
        var query = new string('x', length);

        await service.SearchAsync("movie", $"  {query}  ", CancellationToken.None);

        Assert.Equal(query, Assert.Single(provider.SearchCalls).Query);
    }

    [Fact]
    public async Task SearchDispatchesToSelectedProviderWithTrimmedQueryLimitAndCancellationToken()
    {
        IReadOnlyList<MediaSearchResult> expected =
            [new("42", "movie", "Example", new DateOnly(2020, 1, 1), null)];
        var selected = new StubMediaProvider
        {
            SearchHandler = (_, _, _) => Task.FromResult(expected),
        };
        var other = new StubMediaProvider("other");
        var service = CreateService(selected, other);
        using var cancellation = new CancellationTokenSource();

        var response = await service.SearchAsync(" MOVIE ", "  Example  ", cancellation.Token);

        var call = Assert.Single(selected.SearchCalls);
        Assert.Equal("Example", call.Query);
        Assert.Equal(10, call.MaxResults);
        Assert.Equal(cancellation.Token, call.CancellationToken);
        Assert.Equal(expected.ToArray(), response.Results.ToArray());
        Assert.Empty(other.SearchCalls);
    }

    [Fact]
    public async Task UnknownMediaTypeIsRejectedWithoutCallingRegisteredProvider()
    {
        var provider = new StubMediaProvider();
        var service = CreateService(provider);

        await Assert.ThrowsAsync<UnknownMediaTypeException>(() =>
            service.SearchAsync("unregistered", "Example", CancellationToken.None));

        Assert.Empty(provider.SearchCalls);
    }

    [Fact]
    public async Task NoMatchesReturnsAnEmptyResultInsteadOfAnError()
    {
        var service = CreateService(EmptyProvider());

        var response = await service.SearchAsync("movie", "No matches", CancellationToken.None);

        Assert.Empty(response.Results);
    }

    [Fact]
    public async Task ProviderFailureIsPreservedForTheApiErrorHandler()
    {
        var failure = new ProviderUnavailableException("test-provider", new HttpRequestException());
        var provider = new StubMediaProvider
        {
            SearchHandler = (_, _, _) => Task.FromException<IReadOnlyList<MediaSearchResult>>(failure),
        };

        var actual = await Assert.ThrowsAsync<ProviderUnavailableException>(() =>
            CreateService(provider).SearchAsync("movie", "Example", CancellationToken.None));

        Assert.Same(failure, actual);
    }

    [Fact]
    public async Task CallerCancellationRemainsCancellationInsteadOfAProviderFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new StubMediaProvider
        {
            SearchHandler = (_, _, token) => Task.FromCanceled<IReadOnlyList<MediaSearchResult>>(token),
        };

        var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(provider).SearchAsync("movie", "Example", cancellation.Token));

        Assert.Equal(cancellation.Token, failure.CancellationToken);
    }

    private static SearchService CreateService(params IMediaProvider[] providers) =>
        new(new MediaProviderResolver(providers));

    private static StubMediaProvider EmptyProvider() => new()
    {
        SearchHandler = (_, _, _) => Task.FromResult<IReadOnlyList<MediaSearchResult>>([]),
    };
}
