using Microsoft.EntityFrameworkCore;
using Palette.Api.Data;
using Palette.Api.Models;

namespace Palette.Tests;

public class BookTests {

    private readonly PaletteDbContext _context;

    public BookTests() {
        var options = new DbContextOptionsBuilder<PaletteDbContext>()
              .UseInMemoryDatabase(Guid.NewGuid().ToString())
              .Options;

        _context = new PaletteDbContext(options);
    }

    [Fact]
    public void CanAddBook() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Rise of Zapp", MediaType = "Book"});
        _context.Books.Add(new Book{MediaId = TestMediaId, OpenLibraryId = "0"});

        _context.SaveChanges();


        Assert.Equal(1, _context.Books.Count());
        Assert.Equal(1, _context.Media.Count());
    }

    [Fact]
    public void CanGetBook() {
        Guid TestMediaId = Guid.NewGuid();
        _context.Media.Add(new Media{MediaId = TestMediaId, Title = "Rise of Zapp", MediaType = "Book"});
        _context.Books.Add(new Book{MediaId = TestMediaId, OpenLibraryId = "0"});

        _context.SaveChanges();

        var Book = _context.Books.Find(TestMediaId);

        Assert.NotNull(Book);
        Assert.Equal(TestMediaId, Book.MediaId);
    }
}