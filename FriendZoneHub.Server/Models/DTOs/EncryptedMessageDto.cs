namespace FrirendZoneHub.Server.Models.DTOs
{
    public class EncryptedMessageDto
    {
        public int Id { get; set; }
        public string Content { get; set; } // Krypterad meddelandeinnehåll (Base64-sträng)
        public string Timestamp { get; set; } // ISO 8601-format
        public string Username { get; set; }
    }
}
