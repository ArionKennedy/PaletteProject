using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;

public class UserTests {

    private readonly PaletteDbContext _context;

    public UserTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddUser() {
        _context.Users.Add(new User { Username = "WillRox", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.SaveChanges();

        Assert.Equal(1, _context.Users.Count()); 
    }

    [Fact]
    public void CanGetUser() {
        Guid TestUserId = Guid.NewGuid();

        _context.Users.Add(new User { UserId = TestUserId, Username = "WillRox", Email = "test@test.com", Auth0Id = "auth0|123" });
        _context.SaveChanges();

        var user = _context.Users.Find(TestUserId);

        Assert.NotNull(user);
        Assert.Equal(TestUserId, user.UserId);
  }
}