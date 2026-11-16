using Backend.Providers;
using Backend.Shared.Errors;
using Palette.Tests.TestDoubles;

namespace Palette.Tests.Unit.Providers;

public class MediaProviderResolverTests
{
    [Theory]
    [InlineData("movie")]
    [InlineData("MOVIE")]
    [InlineData("MoViE")]
    public void ResolvesRegisteredTypeWithoutDependingOnCase(string requestedType)
    {
        var provider = new StubMediaProvider();
        var resolver = new MediaProviderResolver([provider]);

        Assert.Same(provider, resolver.Resolve(requestedType));
    }

    [Fact]
    public void UnknownTypeIsRejectedInsteadOfFallingBackToAnotherProvider()
    {
        var resolver = new MediaProviderResolver([new StubMediaProvider()]);

        Assert.Throws<UnknownMediaTypeException>(() => resolver.Resolve("unregistered"));
    }

    [Theory]
    [InlineData("movie")]
    [InlineData("MOVIE")]
    public void DuplicateRegistrationsAreRejectedRegardlessOfCase(string duplicateType)
    {
        Assert.Throws<InvalidOperationException>(() =>
            new MediaProviderResolver([new StubMediaProvider(), new StubMediaProvider(duplicateType)]));
    }

    [Fact]
    public void DifferentMediaTypesResolveToTheirOwnProviders()
    {
        var movie = new StubMediaProvider();
        // A synthetic type exercises extensibility without depending on an unmerged feature.
        var other = new StubMediaProvider("other");
        var resolver = new MediaProviderResolver([movie, other]);

        Assert.Same(movie, resolver.Resolve("movie"));
        Assert.Same(other, resolver.Resolve("other"));
    }
}
