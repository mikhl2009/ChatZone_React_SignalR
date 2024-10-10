using FriendZoneHub.Server.Data;
using FrirendZoneHub.Server.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
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
        [Authorize]
        public async Task<IActionResult> GetChatRooms()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid or missing user ID.");
            }

            var chatRooms = await _context.ChatRooms
                .Include(cr => cr.Admin)
                .Include(cr => cr.Users)
                .Where(cr => !cr.IsPrivate || cr.AdminId == userId || cr.Users.Any(u => u.Id == userId))
                .ToListAsync();

            var chatRoomDtos = chatRooms.Select(cr => new ChatRoomDto
            {
                Id = cr.Id,
                Name = cr.Name,
                IsPrivate = cr.IsPrivate,
                Admin = new UserDto
                {
                    Id = cr.Admin.Id,
                    Username = cr.Admin.Username
                },
                // Optionally include users if needed
                Users = cr.Users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username
                }).ToList()
            }).ToList();

            return Ok(chatRoomDtos);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateChatRoom([FromBody] CreateChatRoomDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized("Invalid or missing user ID.");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var chatRoom = new ChatRoom
            {
                Name = dto.Name,
                IsPrivate = dto.IsPrivate,
                Users = new List<User> { user }, 
                AdminId = user.Id 
            };

            _context.ChatRooms.Add(chatRoom);
            await _context.SaveChangesAsync();

            var chatRoomDto = new ChatRoomDto
            {
                Id = chatRoom.Id,
                Name = chatRoom.Name,
                IsPrivate = chatRoom.IsPrivate,
                Admin = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username
                },
                Users = new List<UserDto>
        {
            new UserDto
            {
                Id = user.Id,
                Username = user.Username
            }
        }
            };

            return CreatedAtAction(nameof(GetChatRoomById), new { id = chatRoom.Id }, chatRoomDto);

        }

        [HttpPost("{id}/addmember")]
        [Authorize]
        public async Task<IActionResult> AddMember(int id, [FromBody] AddMemberDto dto)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid or missing user ID.");
            }

            var chatRoom = await _context.ChatRooms
                .Include(cr => cr.Users)
                .FirstOrDefaultAsync(cr => cr.Id == id);

            if (chatRoom == null)
            {
                return NotFound("Chat room not found.");
            }

            // Check if the current user is the admin
            if (chatRoom.AdminId != currentUserId)
            {
                return Forbid("Only the room admin can add members.");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!chatRoom.Users.Any(u => u.Id == user.Id))
            {
                chatRoom.Users.Add(user);
                await _context.SaveChangesAsync();
                return Ok("User added to chat room.");
            }
            else
            {
                return BadRequest("User is already a member of the chat room.");
            }
        }

        // GetChatRoomById

        [HttpGet("{id}")]
        public async Task<IActionResult> GetChatRoomById(int id)
        {
            var chatRoom = await _context.ChatRooms
                .Include(cr => cr.Admin)
                .Include(cr => cr.Users)
                .FirstOrDefaultAsync(cr => cr.Id == id);

            if (chatRoom == null)
            {
                return NotFound("Chat room not found.");
            }

            var chatRoomDto = new ChatRoomDto
            {
                Id = chatRoom.Id,
                Name = chatRoom.Name,
                IsPrivate = chatRoom.IsPrivate,
                Admin = new UserDto
                {
                    Id = chatRoom.Admin.Id,
                    Username = chatRoom.Admin.Username
                },
                Users = chatRoom.Users.Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username
                }).ToList()
            };

            return Ok(chatRoomDto);
        }
    }
}
