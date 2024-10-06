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
            var chatRoom = await _context.ChatRooms.Include(cr => cr.AllowedUsers)
                                .FirstOrDefaultAsync(cr => cr.Name == roomName);

            if (chatRoom.IsPrivate)
            {
                var userId = Context.User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
                if (!chatRoom.AllowedUsers.Any(u => u.Id == int.Parse(userId)))
                {
                    await Clients.Caller.SendAsync("AccessDenied", "You are not allowed in this room.");
                    return;
                }
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            _logger.LogInformation($"{Context.User.Identity.Name} joined {roomName}");
            await Clients.Group(roomName).SendAsync("ReceiveMessage", $"{Context.User.Identity.Name} joined room {roomName}" );
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
            var user = await _context.Users.FindAsync(int.Parse(userId));

            var chatMessage = new Message
            {
                Content = sanitizedMessage,
                Timestamp = DateTime.UtcNow,
                ChatRoomId = _context.ChatRooms.FirstOrDefault(cr => cr.Name == roomName).Id,
                UserId = user.Id
            };
            _logger.LogInformation($"{Context.User?.Identity?.Name} {roomName} : {sanitizedMessage}");

            _context.Messages.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.Group(roomName).SendAsync("ReceiveMessage", user.Username, sanitizedMessage, DateTime.UtcNow);
            //await Clients.Group(roomName).SendAsync("ReceiveMessage", new
            //{
            //    user = user.Username,
            //    message = sanitizedMessage,  // Meddelandetext
            //    timestamp = chatMessage.Timestamp
            //});
        }
        


    }



}