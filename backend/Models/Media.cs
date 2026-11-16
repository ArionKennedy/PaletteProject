using System.ComponentModel.DataAnnotations;
using Palette.Api.Enums;

namespace Palette.Api.Models;

public class Media
{
    [Key]
    public Guid MediaId { get; set; }
    [Required]
    public string Title { get; set; } = string.Empty;
    public DateOnly? ReleaseDate { get; set; }
    public string? Description { get; set; }
    public string? PosterUrl { get; set; }
    [Required]
    public MediaType MediaType { get; set; }
    
    public Movie? Movie { get; set; }
    public TvShow? TvShow { get; set; }
    public Book? Book { get; set; }
    public Game? Game { get; set; }
}
