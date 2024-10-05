using FriendZoneHub.Server.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FrirendZoneHub.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatAppContext _context;

        public ChatController(ChatAppContext context)
        {
            _context = context;
        }

        [HttpGet("messages/{roomName}")]
        public async Task<IActionResult> GetMessages(string roomName)
        {
            var chatRoom = await _context.ChatRooms.FirstOrDefaultAsync(cr => cr.Name == roomName);
            if (chatRoom == null)
                return NotFound("Chat room not found.");

            var messages = await _context.Messages
                                .Where(m => m.ChatRoomId == chatRoom.Id)
                                .Include(m => m.User)
                                .OrderBy(m => m.Timestamp)
                                .Select(m => new
                                {
                                    Username = m.User.Username,
                                    Content = m.Content,
                                    Timestamp = m.Timestamp
                                })
                                .ToListAsync();

            return Ok(messages);
        }
    }
}
