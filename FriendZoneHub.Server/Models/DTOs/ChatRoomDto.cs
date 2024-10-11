namespace FrirendZoneHub.Server.Models.DTOs
{
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsPrivate { get; set; }
        public UserDto Admin { get; set; }
        public List<UserDto> Users { get; set; }
    }
}
