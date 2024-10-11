using FrirendZoneHub.Server.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriendZoneHub.Tests
{
    public class EncryptionHelperTests
    {
        [Fact]
        public void Encrypt_ShouldReturnEncryptedString()
        {
            // Arrange
            var plaintext = "HelloWorld";
            var logger = new Mock<ILogger<EncryptionHelper>>().Object;
            var helper = new EncryptionHelper(logger);

            // Act
            var encryptedText = helper.Encrypt(plaintext);

            // Assert
            Assert.NotNull(encryptedText);
            Assert.NotEqual(plaintext, encryptedText);
        }

        [Fact]
        public void Decrypt_ShouldReturnOriginalString()
        {
            // Arrange
            var plaintext = "HelloWorld";
            var logger = new Mock<ILogger<EncryptionHelper>>().Object;
            var helper = new EncryptionHelper(logger);
            var encryptedText = helper.Encrypt(plaintext);

            // Act
            var decryptedText = helper.Decrypt(encryptedText);

            // Assert
            Assert.Equal(plaintext, decryptedText);
        }
    }
}
