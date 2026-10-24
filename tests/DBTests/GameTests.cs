using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class GameTests {

    private readonly PaletteDbContext _context;

    public GameTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddGame() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Saulo strikes back", MediaType = "Game"});
        _context.Games.Add(new Game{MediaId = TestMediaId, RawgGameId = 0});

        _context.SaveChanges();


        Assert.Equal(1, _context.Games.Count());
        Assert.Equal(1, _context.Media.Count());
    }

    [Fact]
    public void CanGetGame() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Saulo strikes back", MediaType = "Game"});
        _context.Games.Add(new Game{MediaId = TestMediaId, RawgGameId = 0});

        _context.SaveChanges();

        var Game = _context.Games.Find(TestMediaId);

        Assert.NotNull(Game);
        Assert.Equal(TestMediaId, Game.MediaId);
    }
}