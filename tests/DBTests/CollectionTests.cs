using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class CollectionTests {

    private readonly PaletteDbContext _context;

    public CollectionTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddCollection() {
        Guid TestUserId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Collections.Add(new Collection { UserId = TestUserId, Name = "Favourites" });

        _context.SaveChanges();

        Assert.Equal(1, _context.Collections.Count());
    }

    [Fact]
    public void CanGetCollection() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestCollectionId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Collections.Add(new Collection { CollectionId = TestCollectionId, UserId = TestUserId, Name = "Favourites" });

        _context.SaveChanges();

        var collection = _context.Collections.Find(TestCollectionId);

        Assert.NotNull(collection);
        Assert.Equal(TestCollectionId, collection.CollectionId);
    }
}