using System.ComponentModel.DataAnnotations;

namespace FriendZoneHub.Server.Models
{
    public class ChatRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public bool IsPrivate { get; set; }

        // Navigation properties
        public ICollection<User> Users { get; set; }
        public ICollection<Message> Messages { get; set; }
    }
}
