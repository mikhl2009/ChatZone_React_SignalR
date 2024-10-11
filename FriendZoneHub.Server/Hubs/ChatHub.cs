using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Models;
using FrirendZoneHub.Server.Models.DTOs;
using FrirendZoneHub.Server.Utils;
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
        private readonly EncryptionHelper _encryptionHelper;

        public ChatHub(ChatAppContext context, ILogger<ChatHub> logger, EncryptionHelper encryptionHelper)
        {
            _context = context;
            _logger = logger;
            _encryptionHelper = encryptionHelper;
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

            // Fetch message history, encrypt each message
            var history = await GetEncryptedMessageHistory(roomName);
            await Clients.Caller.SendAsync("ReceiveMessageHistory", history);

            // Notify others in the room with an encrypted system message
            string systemMessage = $"{user.Username} joined the room {roomName}";
            string encryptedSystemMessage = _encryptionHelper.Encrypt(systemMessage);

            await Clients.Group(roomName).SendAsync(
                "ReceiveMessage",
                "System",
                encryptedSystemMessage,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            _logger.LogInformation($"{user.Username} joined room {roomName}.");
        }

        public async Task LeaveRoom(string roomName)
        {
            var userIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogError("Invalid or missing user ID.");
                return;
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogError($"User not found with ID: {userId}");
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            _logger.LogInformation($"{user.Username} left {roomName}");

            // Notify others in the room with an encrypted system message
            string systemMessage = $"{user.Username} has left the room {roomName}";
            string encryptedSystemMessage = _encryptionHelper.Encrypt(systemMessage);
            await Clients.Group(roomName).SendAsync(
                "ReceiveMessage",
                "System",
                encryptedSystemMessage,
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }

        public async Task SendMessage(string roomName, string encryptedMessage)
        {
            var userIdClaim = Context.User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogError("Invalid or missing user ID.");
                return;
            }

            var user = await _context.Users.FindAsync(userId);
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

            bool isUserAuthorized = !chatRoom.IsPrivate || chatRoom.Users.Any(u => u.Id == user.Id) || chatRoom.AdminId == user.Id;
            if (!isUserAuthorized)
            {
                _logger.LogWarning($"Access denied for user {user.Username} to send message to room {roomName}");
                await Clients.Caller.SendAsync("AccessDenied", "You do not have access to this room.");
                return;
            }

            // Decrypt the incoming message
            string decryptedMessage = _encryptionHelper.Decrypt(encryptedMessage);
            if (string.IsNullOrEmpty(decryptedMessage))
            {
                _logger.LogError("Failed to decrypt message.");
                await Clients.Caller.SendAsync("Error", "Failed to decrypt your message.");
                return;
            }

            // Sanitize the decrypted message
            var sanitizer = new HtmlSanitizer();
            var sanitizedMessage = sanitizer.Sanitize(decryptedMessage);

            var chatMessage = new Message
            {
                Content = _encryptionHelper.Encrypt(sanitizedMessage), // Kryptera meddelandet innan det sparas
                Timestamp = DateTime.UtcNow,
                ChatRoomId = chatRoom.Id,
                UserId = user.Id
            };
            _logger.LogInformation($"{user.Username} in {roomName}: {sanitizedMessage}");

            _context.Messages.Add(chatMessage);
            await _context.SaveChangesAsync();

            // Encrypt the message again before sending to clients
            string encryptedForBroadcast = _encryptionHelper.Encrypt(sanitizedMessage);

            await Clients.Group(roomName).SendAsync(
                "ReceiveMessage",
                user.Username,
                encryptedForBroadcast,
                chatMessage.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
        public async Task<List<EncryptedMessageDto>> GetEncryptedMessageHistory(string roomName)
        {
            var messages = await _context.Messages
                .Where(m => m.ChatRoom.Name == roomName)
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .Include(m => m.User)
                .ToListAsync();

            // Order messages ascendingly for proper history display
            messages = messages.OrderBy(m => m.Timestamp).ToList();

            var encryptedHistory = new List<EncryptedMessageDto>();

            foreach (var m in messages)
            {
                // Do not re-encrypt the content here; use the stored encrypted value
                encryptedHistory.Add(new EncryptedMessageDto
                {
                    Id = m.Id,
                    Content = m.Content, // Use the encrypted content directly from the database
                    Timestamp = m.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    Username = m.User.Username
                });
            }

            return encryptedHistory;
        }


        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Hantera avregistrering från grupper om nödvändigt
            await base.OnDisconnectedAsync(exception);
        }
    }
}
