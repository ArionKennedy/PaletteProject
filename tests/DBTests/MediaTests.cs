using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;


public class MediaTests {

    private readonly PaletteDbContext _context;

    public MediaTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddMedia() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Media"});

        _context.SaveChanges();

        Assert.Equal(1, _context.Media.Count());
    }

    [Fact]
    public void CanGetMedia() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Revenge of Guderian", MediaType = "Media"});

        _context.SaveChanges();

        var Media = _context.Media.Find(TestMediaId);

        Assert.NotNull(Media);
        Assert.Equal(TestMediaId, Media.MediaId);
    }
}