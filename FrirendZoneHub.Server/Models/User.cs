// Models/User.cs
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    public string ProfilePictureUrl { get; set; }

    [Required]
    public string Email { get; set; }
    
}