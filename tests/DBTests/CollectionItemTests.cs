using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class CollectionItemTests {

    private readonly PaletteDbContext _context;

    public CollectionItemTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
        public void CanAddCollectionItem() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestCollectionId = Guid.NewGuid();
        Guid TestMediaId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Collections.Add(new Collection { CollectionId = TestCollectionId, UserId = TestUserId, Name = "My Favourites" });
        _context.Media.Add(new Media { MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie" });
        _context.CollectionItems.Add(new CollectionItem { CollectionId = TestCollectionId, MediaId = TestMediaId });

        _context.SaveChanges();

        Assert.Equal(1, _context.CollectionItems.Count());
    }

    [Fact]
    public void CanGetCollectionItem() {
        Guid TestUserId = Guid.NewGuid();
        Guid TestCollectionId = Guid.NewGuid();
        Guid TestMediaId = Guid.NewGuid();
        _context.Users.Add(new User { UserId = TestUserId, Username = "test", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.Collections.Add(new Collection { CollectionId = TestCollectionId, UserId = TestUserId, Name = "My Favourites" });
        _context.Media.Add(new Media { MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Movie" });
        _context.CollectionItems.Add(new CollectionItem { CollectionId = TestCollectionId, MediaId = TestMediaId });

        _context.SaveChanges();

        var item = _context.CollectionItems.Find(TestCollectionId, TestMediaId);

        Assert.NotNull(item);
        Assert.Equal(TestCollectionId, item.CollectionId);
    }

}