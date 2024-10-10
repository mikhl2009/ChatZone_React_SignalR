using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Models;
using FrirendZoneHub.Server.Models.DTOs;
using Ganss.Xss;
 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace FriendZoneHub.Server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatAppContext _context;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ChatAppContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task JoinRoom(string roomName)
        {
            var userIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogError("Invalid or missing user ID.");
                return;
            }

            var user = await _context.Users
                .Include(u => u.ChatRooms)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogError($"User not found with ID: {userId}");
                return;
            }

            var chatRoom = await _context.ChatRooms
                .Include(cr => cr.Users)
                .FirstOrDefaultAsync(cr => cr.Name == roomName);

            if (chatRoom == null)
            {
                _logger.LogError($"Chat room not found: {roomName}");
                return;
            }

            // Check if the chat room is private and if the user has access
            bool isUserAuthorized = chatRoom.Users.Any(u => u.Id == userId) || chatRoom.AdminId == userId;

            if (chatRoom.IsPrivate && !isUserAuthorized)
            {
                _logger.LogWarning($"Access denied for user {user.Username} to room {roomName}");
                await Clients.Caller.SendAsync("AccessDenied", "You do not have access to this room.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);


            // Send message history
            var history = await GetMessageHistory(roomName);
            await Clients.Caller.SendAsync("ReceiveMessageHistory", history);

            // Notify others in the room
            await Clients.Group(roomName).SendAsync
                ("ReceiveMessage",null,$"{user.Username} joined the room {roomName}",
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }

        // get chat room by Id  
        

        //public async Task AddUserToRoom(int userId, string roomName)
        //{
            
        //    var chatRoom = await _context.ChatRooms
        //        .Include(cr => cr.Users)
        //        .FirstOrDefaultAsync(cr => cr.Name == roomName);

        //    if (chatRoom == null)
        //    {
        //        _logger.LogError($"Chat room not found: {roomName}");
        //        return;
        //    }

        //    var user = await _context.Users.FindAsync(userId);
        //    if (user == null)
        //    {
        //        _logger.LogError($"User not found with ID: {userId}");
        //        return;
        //    }

        //    if (!chatRoom.Users.Any(u => u.Id == userId))
        //    {
        //        chatRoom.Users.Add(user);
        //        await _context.SaveChangesAsync();
        //        _logger.LogInformation($"User {user.Username} added to room {roomName}");
        //    }
        //}
        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            _logger.LogInformation($"{Context.User.Identity.Name} left {roomName}");
        }

        public async Task SendMessage(string roomName, string message)
        {
            var sanitizer = new HtmlSanitizer();
            var sanitizedMessage = sanitizer.Sanitize(message);

            var userId = Context.User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
            {
                _logger.LogError("Invalid or missing user ID.");
                return;
            }

            var user = await _context.Users.FindAsync(parsedUserId);
            if (user == null)
            {
                _logger.LogError($"User not found with ID: {parsedUserId}");
                return;
            }

            var chatRoom = _context.ChatRooms
                .Include(cr => cr.Users)
                .FirstOrDefault(cr => cr.Name == roomName);
            if (chatRoom == null)
            {
                _logger.LogError($"Chat room not found: {roomName}");
                return;
            }
            bool isUserAuthorized = !chatRoom.IsPrivate || chatRoom.Users.Any(u => u.Id == user.Id) || chatRoom.AdminId == user.Id;
            if (!isUserAuthorized)
            {
                _logger.LogWarning($"Access denied for user {user.Username} to send message to room {roomName}");
                await Clients.Caller.SendAsync("AccessDenied", "You do not have access to this room.");
                return;
            }
            var chatMessage = new Message
            {
                Content = sanitizedMessage,
                Timestamp = DateTime.UtcNow,
                ChatRoomId = chatRoom.Id,
                UserId = user.Id
            };
            _logger.LogInformation($"{user.Username} in {roomName}: {message}");

            _context.Messages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Group(roomName).SendAsync("ReceiveMessage", user.Username, sanitizedMessage, chatMessage.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }

        public async Task<List<MessageDto>> GetMessageHistory(string roomName)
        {
            return await _context.Messages
                .Where(m => m.ChatRoom.Name == roomName)
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .Select(m => new MessageDto
                {
                    Id = m.Id,
                    Content = m.Content,
                    Timestamp = m.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"), // Format as ISO 8601 string
                    Username = m.User.Username
                })
                .ToListAsync();
        }




    }



}
