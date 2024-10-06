using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Models;
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
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
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

            var systemMessage = $"{user.Username} joined the room {roomName}";
            var timestamp = DateTime.UtcNow.ToString("o");

            // Send a system message with user as "System"
            await Clients.Group(roomName).SendAsync("ReceiveMessage", $"{Context.User.Identity.Name} joined the room {roomName}");
        }

        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            _logger.LogInformation($"{Context.User.Identity.Name} left {roomName}");
        }

        public async Task SendMessage(string roomName, string message)
        {
            // Sanitize message to prevent XSS
            var sanitizedMessage = System.Net.WebUtility.HtmlEncode(message);

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

            var chatRoom = _context.ChatRooms.FirstOrDefault(cr => cr.Name == roomName);
            if (chatRoom == null)
            {
                _logger.LogError($"Chat room not found: {roomName}");
                return;
            }

            var chatMessage = new Message
            {
                Content = sanitizedMessage,
                Timestamp = DateTime.UtcNow,
                ChatRoomId = chatRoom.Id,
                UserId = user.Id
            };
            _logger.LogInformation($"{user.Username} in {roomName}: {sanitizedMessage}");

            _context.Messages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Group(roomName).SendAsync("ReceiveMessage", user.Username, sanitizedMessage, chatMessage.Timestamp.ToString("o"));
        }





    }



}
