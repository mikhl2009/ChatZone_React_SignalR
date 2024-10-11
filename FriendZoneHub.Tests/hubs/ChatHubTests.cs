using FluentAssertions;
using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Hubs;
using FriendZoneHub.Server.Models;
using FrirendZoneHub.Server.Models.DTOs;
using FrirendZoneHub.Server.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

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
            SeedDatabase();

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

            // Mock HubCallerContext with ClaimsPrincipal
            var mockHubCallerContext = new Mock<HubCallerContext>();
            var claims = new List<Claim>
            {
                new Claim("uid", "1") // Assume user with ID 1 is connected
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var user = new ClaimsPrincipal(identity);
            mockHubCallerContext.Setup(c => c.User).Returns(user);
            mockHubCallerContext.Setup(c => c.ConnectionId).Returns("TestConnectionId");

            // Instantiate ChatHub with mocked dependencies
            _chatHub = new ChatHub(_context, _mockLogger.Object, new EncryptionHelper())
            {
                Clients = _mockClients.Object,
                Context = mockHubCallerContext.Object,
                Groups = _mockGroups.Object
            };
        }

        private void SeedDatabase()
        {
            // Seed Users
            var users = new List<User>
            {
                new User { Id = 1, Username = "testuser1", PasswordHash = "hashedpwd1", Email = "user1@example.com" },
                new User { Id = 2, Username = "testuser2", PasswordHash = "hashedpwd2", Email = "user2@example.com" }
            };
            _context.Users.AddRange(users);

            // Seed ChatRooms
            var chatRooms = new List<ChatRoom>
            {
                new ChatRoom { Id = 1, Name = "General", IsPrivate = false, AdminId = 1, Users = new List<User> { users[0] } },
                new ChatRoom { Id = 2, Name = "PrivateRoom", IsPrivate = true, AdminId = 2, Users = new List<User> { users[1] } }
            };
            _context.ChatRooms.AddRange(chatRooms);
            _context.SaveChanges();
        }

        [Fact]
        public async Task JoinRoom_ShouldAddUserToGroup_AndSendMessageHistory()
        {
            // Arrange
            string roomName = "General";

            // Act
            await _chatHub.JoinRoom(roomName);

            // Assert
            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("joined room")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockCallerClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessageHistory",
                    It.Is<object?[]>(args => args.Length == 1 && args[0] is List<EncryptedMessageDto>),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockGroupClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessage",
                    It.Is<object?[]>(args => args.Length == 3
                        && args[0] == null
                        && args[1].ToString().Contains("joined the room")
                        && args[2] is string),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task LeaveRoom_ShouldRemoveUserFromGroup_AndNotifyOthers()
        {
            // Arrange
            string roomName = "General";

            // Act
            await _chatHub.LeaveRoom(roomName);

            // Assert
            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("left")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _mockGroupClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessage",
                    It.Is<object?[]>(args => args.Length == 3
                        && args[0] == null
                        && args[1].ToString().Contains("has left the room")
                        && args[2] is string),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SendMessage_ShouldBroadcastMessage_AndSaveToDatabase()
        {
            // Arrange
            string roomName = "General";
            string messageContent = "Test message";
            string encryptedMessage = EncryptionHelper.Encrypt(messageContent); // Ensure this is valid

            // Act
            await _chatHub.SendMessage(roomName, encryptedMessage);

            // Assert
            var savedMessage = _context.Messages.FirstOrDefault(m => m.ChatRoomId == 1 && m.UserId == 1);
            savedMessage.Should().NotBeNull("because the message should have been added to the database");
            savedMessage.Content.Should().NotBe(messageContent, "because the content should be encrypted"); // Check that it's not the plain text
            savedMessage.Content.Should().Be(encryptedMessage, "because the content should match the encrypted message");

            _mockGroupClientProxy.Verify(
                client => client.SendCoreAsync("ReceiveMessage",
                    It.Is<object?[]>(args => args.Length == 3
                        && args[0].ToString() == "testuser1"
                        && args[1].ToString() == encryptedMessage // Ensure this matches the encrypted message
                        && args[2] is string),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _mockLogger.Verify(
                logger => logger.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("in General: Test message")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task GetEncryptedMessageHistory_ShouldReturnMessageHistory()
        {
            // Arrange
            string roomName = "General";

            // Act
            var result = await _chatHub.GetEncryptedMessageHistory(roomName);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<List<EncryptedMessageDto>>();
            result.Count.Should().BeGreaterThan(0, "because there should be messages in the history");
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}