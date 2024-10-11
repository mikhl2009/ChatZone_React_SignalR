using FluentAssertions;
using FriendZoneHub.Server.Data;
using FriendZoneHub.Server.Hubs;
using FriendZoneHub.Server.Models;
using FrirendZoneHub.Server.Utils;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FriendZoneHub.Tests.Hubs
{
    public class ChatHubTests
    {
        private readonly Mock<IHubCallerClients> _mockClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly Mock<ILogger<ChatHub>> _mockLogger;
        private readonly ChatHub _chatHub;
        private readonly ChatAppContext _context;

        public ChatHubTests()
        {
            // Setup In-Memory Database for ChatAppContext
            var options = new DbContextOptionsBuilder<ChatAppContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;
            _context = new ChatAppContext(options);

            // Seed data if needed
            SeedDatabase();

            // Mock SignalR Clients
            _mockClients = new Mock<IHubCallerClients>();
            _mockClientProxy = new Mock<IClientProxy>();
            _mockClients.Setup(clients => clients.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);

            // Mock ILogger<ChatHub>
            _mockLogger = new Mock<ILogger<ChatHub>>();

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
            _chatHub = new ChatHub(_context, _mockLogger.Object, new EncryptionHelper(new Mock<ILogger<EncryptionHelper>>().Object))
            {
                Clients = _mockClients.Object,
                Context = mockHubCallerContext.Object
            };
        }

        private void SeedDatabase()
        {
            _context.Users.Add(new User { Id = 1, Username = "testuser1", Email = "testuser1@example.com", PasswordHash = "hashedpassword" });
            _context.ChatRooms.Add(new ChatRoom { Id = 1, Name = "General", IsPrivate = false });
            _context.SaveChanges();
        }

        [Fact]
        public async Task SendMessage_ShouldBroadcastMessage_ToAllClients()
        {
            // Arrange
            string roomName = "General";
            string message = "Hello, world!";
            var encryptionHelper = new EncryptionHelper(new Mock<ILogger<EncryptionHelper>>().Object);
            var encryptedMessage = encryptionHelper.Encrypt(message);

            // Act
            await _chatHub.SendMessage(roomName, encryptedMessage);

            // Assert
            _mockClientProxy.Verify(
                client => client.SendCoreAsync(
                    "ReceiveMessage",
                    It.Is<object[]>(args => args != null && args.Length == 3 && args[1].ToString() == encryptedMessage),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task JoinRoom_ShouldAddUserToGroup()
        {
            // Arrange
            string roomName = "General";

            // Mock Groups
            var mockGroups = new Mock<IGroupManager>();
            mockGroups.Setup(g => g.AddToGroupAsync(It.IsAny<string>(), roomName, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            _chatHub.Groups = mockGroups.Object;

            // Act
            await _chatHub.JoinRoom(roomName);

            // Assert
            mockGroups.Verify(
                g => g.AddToGroupAsync(It.Is<string>(connectionId => connectionId == "TestConnectionId"),
                It.Is<string>(group => group == roomName),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task LeaveRoom_ShouldRemoveUserFromGroup()
        {
            // Arrange
            string roomName = "General";

            // Mock Groups
            var mockGroups = new Mock<IGroupManager>();
            mockGroups.Setup(g => g.RemoveFromGroupAsync(It.IsAny<string>(), roomName, It.IsAny<CancellationToken>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

            _chatHub.Groups = mockGroups.Object;

            // Act
            await _chatHub.LeaveRoom(roomName);

            // Assert
            mockGroups.Verify(
                g => g.RemoveFromGroupAsync(It.Is<string>(connectionId => connectionId == "TestConnectionId"),
                It.Is<string>(group => group == roomName),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
