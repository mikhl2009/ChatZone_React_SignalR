using FriendZoneHub.Server.Models;
using System.ComponentModel.DataAnnotations;

public class ChatRoom
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; }

    public bool IsPrivate { get; set; }

    // Navigation properties
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
