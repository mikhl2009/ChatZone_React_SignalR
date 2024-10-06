using FriendZoneHub.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FrirendZoneHub.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatRoomsController : Controller
    {
        private readonly ChatAppContext _context;

        public ChatRoomsController(ChatAppContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetChatRooms()
        {
            var chatRooms = await _context.ChatRooms.ToListAsync();
            return Ok(chatRooms);
        }
    }
}
