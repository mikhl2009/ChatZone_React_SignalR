using System.ComponentModel.DataAnnotations;

namespace FriendZoneHub.Server.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; }

        // Foreign keys
        public int ChatRoomId { get; set; }
        public ChatRoom ChatRoom { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }
    }
}
