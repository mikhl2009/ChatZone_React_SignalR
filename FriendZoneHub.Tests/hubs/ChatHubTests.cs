using FluentAssertions;
using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Hubs;
using FriendZoneHub.Server.Models;
using FrirendZoneHub.Server.Models.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;

namespace FriendZoneHub.Tests.Hubs
{
    public class ChatHubTests : IDisposable
    {
        private readonly ChatAppContext _context;
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<IClientProxy> _mockAllClientProxy;
        private readonly Mock<ISingleClientProxy> _mockCallerClientProxy;
        private readonly Mock<IClientProxy> _mockGroupClientProxy;
        private readonly Mock<IGroupManager> _mockGroups;
        private readonly Mock<ILogger<ChatHub>> _mockLogger;
        private readonly ChatHub _chatHub;

        public ChatHubTests()
        {
            // Setup In-Memory Database
            var options = new DbContextOptionsBuilder<ChatAppContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ChatAppContext(options);
            SeedDatabase(_context);

            // Mock SignalR Clients
            _mockClients = new Mock<IHubCallerClients>();
            _mockAllClientProxy = new Mock<IClientProxy>();
            _mockCallerClientProxy = new Mock<ISingleClientProxy>();
            _mockGroupClientProxy = new Mock<IClientProxy>();

            _mockClients.Setup(clients => clients.All).Returns(_mockAllClientProxy.Object);
            _mockClients.Setup(clients => clients.Caller).Returns(_mockCallerClientProxy.Object);
            _mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_mockGroupClientProxy.Object);

            // Mock ILogger<ChatHub>
            _mockLogger = new Mock<ILogger<ChatHub>>();

            // Mock IGroupManager
            _mockGroups = new Mock<IGroupManager>();
            _mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Mock HubCallerContext med ClaimsPrincipal
            var mockHubCallerContext = new Mock<HubCallerContext>();
            var claims = new List<Claim>
            {
                new Claim("uid", "1") // anta att användare med ID 1 är ansluten
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            mockHubCallerContext.Setup(c => c.User).Returns(user);
            mockHubCallerContext.Setup(c => c.ConnectionId).Returns("TestConnectionId");

            // Instansiera ChatHub med mockade beroenden
            _chatHub = new ChatHub(_context, _mockLogger.Object)
            {
                Clients = _mockClients.Object,
                Context = mockHubCallerContext.Object,
                Groups = _mockGroups.Object
            };
        }


        private void SeedDatabase(ChatAppContext context)
        {
            // Seed Users
            var users = new List<User>
            {
                new User { Id = 1, Username = "testuser1", PasswordHash = "hashedpwd1", Email = "user1@example.com" },
                new User { Id = 2, Username = "testuser2", PasswordHash = "hashedpwd2", Email = "user2@example.com" }
            };
            context.Users.AddRange(users);

            // Seed ChatRooms
            var chatRooms = new List<ChatRoom>
            {
                new ChatRoom { Id = 1, Name = "General", IsPrivate = false, AdminId = 1 },
                new ChatRoom { Id = 2, Name = "PrivateRoom", IsPrivate = true, AdminId = 2 }
            };
            context.ChatRooms.AddRange(chatRooms);

            // Seed UserChatRooms via navigations
            var generalRoom = chatRooms.First(cr => cr.Name == "General");
            var privateRoom = chatRooms.First(cr => cr.Name == "PrivateRoom");

            var user1 = users.First(u => u.Id == 1);
            var user2 = users.First(u => u.Id == 2);

            user1.ChatRooms.Add(generalRoom);
            user2.ChatRooms.Add(generalRoom);
            user2.ChatRooms.Add(privateRoom);

            // Seed Messages
            var messages = new List<Message>
            {
                new Message { Id = 1, Content = "Hello World!", Timestamp = DateTime.UtcNow.AddMinutes(-10), ChatRoomId = 1, UserId = 1 },
                new Message { Id = 2, Content = "Hi there!", Timestamp = DateTime.UtcNow.AddMinutes(-5), ChatRoomId = 1, UserId = 2 }
            };
            context.Messages.AddRange(messages);

            context.SaveChanges();
        }

        /// <summary>
        /// Test 1: Anslutning till Ett Chattrum
        /// Säkerställer att en användare kan ansluta till ett chattrum, får meddelandehistorik och att rätt loggnivå används.
        /// </summary>
        [Fact]
        public async Task JoinRoom_ShouldAddUserToGroup_AndSendMessageHistory()
        {
            // Arrange
            string roomName = "General";

            // Act
            await _chatHub.JoinRoom(roomName);

            // Assert
            // Verifiera att rätt chattrum hämtades från databasen genom att kontrollera loggen
            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("joined room")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            // Verifiera att meddelandehistoriken skickades till klienten
            _mockCallerClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessageHistory",
                    It.Is<object?[]>(args => args.Length == 1 && args[0] is List<MessageDto>),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verifiera att ett meddelande skickades till gruppen om att användaren gick med
            _mockGroupClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessage",
                    It.Is<object?[]>(args => args.Length == 3
                        && args[0] == null
                        && args[1].ToString().Contains("joined the room")
                        && args[2] is string),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Test 2: Meddelandesändning
        /// Säkerställer att ett meddelande sparas i databasen och broadcastas korrekt till gruppen.
        /// </summary>
        [Fact]
        public async Task SendMessage_ShouldBroadcastMessage_AndSaveToDatabase()
        {
            // Arrange
            string roomName = "General";
            string messageContent = "Test message";

            // Act
            await _chatHub.SendMessage(roomName, messageContent);

            // Assert
            // Verifiera att meddelandet lades till i databasen
            _context.Messages.Any(m => m.Content == messageContent && m.ChatRoomId == 1 && m.UserId == 1)
                .Should().BeTrue("because the message should have been added to the database");

            // Verifiera att meddelandet broadcastades till gruppen
            _mockGroupClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessage",
                    It.Is<object?[]>(args => args.Length == 3
                        && args[0].ToString() == "testuser1"
                        && args[1].ToString() == messageContent
                        && args[2] is string),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verifiera att en informationslogg skapades när meddelandet skickades
            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("in General: Test message")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Test 3.1: Felhantering - Ogiltigt Användar-ID
        /// Säkerställer att om en användare försöker skicka ett meddelande med ett ogiltigt användar-ID, hanteras felet korrekt.
        /// </summary>
        [Fact]
        public async Task SendMessage_InvalidUserId_ShouldLogError_AndNotBroadcast()
        {
            // Arrange
            string roomName = "General";
            string messageContent = "Test message with invalid user";

            // Mock context med ogiltigt användar-ID
            var mockHubCallerContext = new Mock<HubCallerContext>();
            var claims = new List<Claim>
            {
                new Claim("uid", "invalid") // Ogiltigt user ID
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            mockHubCallerContext.Setup(c => c.User).Returns(user);
            mockHubCallerContext.Setup(c => c.ConnectionId).Returns("TestConnectionId");

            // Uppdatera ChatHub-kontakten
            _chatHub.Context = mockHubCallerContext.Object;

            // Act
            await _chatHub.SendMessage(roomName, messageContent);

            // Assert
            // Verifiera att meddelandet inte sparades i databasen
            _context.Messages.Any(m => m.Content == messageContent).Should().BeFalse("because the user ID was invalid");

            // Verifiera att ingen broadcast skedde
            _mockGroupClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessage",
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            // Verifiera att ett fel loggades
            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Invalid or missing user ID.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Test 3.2: Felhantering - Otillåten Åtkomst till Privat Chattrum
        /// Säkerställer att om en användare utan behörighet försöker skicka ett meddelande till ett privat chattrum, hanteras detta korrekt.
        /// </summary>
        [Fact]
        public async Task SendMessage_UnauthorizedUser_ShouldLogWarning_AndSendAccessDenied()
        {
            // Arrange
            string roomName = "PrivateRoom"; // Endast användare med ID 2 har åtkomst
            string messageContent = "Unauthorized message";

            // Act
            await _chatHub.SendMessage(roomName, messageContent);

            // Assert
            // Verifiera att meddelandet inte sparades i databasen
            _context.Messages.Any(m => m.Content == messageContent).Should().BeFalse("because the user is unauthorized");

            // Verifiera att AccessDenied skickades till klienten via Caller
            _mockCallerClientProxy.Verify(
                client => client.SendCoreAsync("AccessDenied",
                    It.Is<object?[]>(args => args.Length == 1 && args[0].ToString() == "You do not have access to this room."),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verifiera att en varning loggades
            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Access denied for user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // Dispose-metod för att rensa upp in-memory databasen efter tester
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
