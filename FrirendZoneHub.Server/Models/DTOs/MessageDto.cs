namespace FrirendZoneHub.Server.Models.DTOs
{
    public class MessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string Username { get; set; } // You can include the username directly
    }
}
