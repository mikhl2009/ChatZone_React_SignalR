using FriendZoneHub.Server.Models;
using System.ComponentModel.DataAnnotations;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; }

    [Required]
    public string PasswordHash { get; set; }

    [Required]
    public string Email { get; set; }

    // Navigation properties
    public ICollection<ChatRoom> ChatRooms { get; set; } = new List<ChatRoom>(); // For many-to-many relationship
}
