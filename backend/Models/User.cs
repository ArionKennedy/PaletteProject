using System.ComponentModel.DataAnnotations;

namespace Palette.Api.Models;

public class User
{
    [Key]
    public Guid UserId { get; set; }
    [Required]
    public string ClerkId { get; set; } = string.Empty;
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;

    public DateTime DateJoined { get; set; }
}