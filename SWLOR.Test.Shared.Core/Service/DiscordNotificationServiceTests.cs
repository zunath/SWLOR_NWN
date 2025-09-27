using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Abstractions.Models;
using SWLOR.Shared.Core.Service;
using System.Drawing;

namespace SWLOR.Test.Shared.Core.Service
{
    [TestFixture]
    public class DiscordNotificationServiceTests
    {
        private IAppSettings _mockAppSettings;
        private DiscordNotificationService _discordService;

        [SetUp]
        public void SetUp()
        {
            _mockAppSettings = Substitute.For<IAppSettings>();
            _mockAppSettings.DMShoutWebHookUrl.Returns("https://discord.com/api/webhooks/test-dm-shout");
            _mockAppSettings.BugWebHookUrl.Returns("https://discord.com/api/webhooks/test-bug");
            _mockAppSettings.HolonetWebHookUrl.Returns("https://discord.com/api/webhooks/test-holonet");
            
            _discordService = new DiscordNotificationService(_mockAppSettings);
        }

        [Test]
        public void PublishMessage_WithDMShoutType_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test DM shout message";
            var color = Color.Red;
            const DiscordNotificationType type = DiscordNotificationType.DMShout;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithBugType_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test bug report";
            var color = Color.Orange;
            const DiscordNotificationType type = DiscordNotificationType.Bug;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithHolonetType_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test holonet message";
            var color = Color.Blue;
            const DiscordNotificationType type = DiscordNotificationType.Holonet;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithTitle_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with title";
            var color = Color.Green;
            const DiscordNotificationType type = DiscordNotificationType.DMShout;
            const string title = "Test Title";

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title));
        }

        [Test]
        public void PublishMessage_WithFields_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with fields";
            var color = Color.Purple;
            const DiscordNotificationType type = DiscordNotificationType.Bug;
            const string title = "Test Title";
            var fields = new List<DiscordNotificationField>
            {
                new DiscordNotificationField { Name = "Field1", Value = "Value1", IsInline = true },
                new DiscordNotificationField { Name = "Field2", Value = "Value2", IsInline = false }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title, fields));
        }

        [Test]
        public void PublishMessage_WithEmptyAuthor_ShouldNotThrow()
        {
            // Arrange
            const string author = "";
            const string message = "Test message with empty author";
            var color = Color.Yellow;
            const DiscordNotificationType type = DiscordNotificationType.Holonet;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithEmptyMessage_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "";
            var color = Color.Cyan;
            const DiscordNotificationType type = DiscordNotificationType.DMShout;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithNullAuthor_ShouldNotThrow()
        {
            // Arrange
            string author = null;
            const string message = "Test message with null author";
            var color = Color.Magenta;
            const DiscordNotificationType type = DiscordNotificationType.Bug;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithNullMessage_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            string message = null;
            var color = Color.Lime;
            const DiscordNotificationType type = DiscordNotificationType.Holonet;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithNullTitle_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with null title";
            var color = Color.Navy;
            const DiscordNotificationType type = DiscordNotificationType.DMShout;
            string title = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title));
        }

        [Test]
        public void PublishMessage_WithNullFields_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with null fields";
            var color = Color.Olive;
            const DiscordNotificationType type = DiscordNotificationType.Bug;
            const string title = "Test Title";
            List<DiscordNotificationField> fields = null;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title, fields));
        }

        [Test]
        public void PublishMessage_WithEmptyFields_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with empty fields";
            var color = Color.Pink;
            const DiscordNotificationType type = DiscordNotificationType.Holonet;
            const string title = "Test Title";
            var fields = new List<DiscordNotificationField>();

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title, fields));
        }

        [Test]
        public void PublishMessage_WithLongMessage_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            var message = new string('A', 2000); // Very long message
            var color = Color.Silver;
            const DiscordNotificationType type = DiscordNotificationType.DMShout;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithSpecialCharacters_ShouldNotThrow()
        {
            // Arrange
            const string author = "Test@Author#123";
            const string message = "Test message with special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?`~";
            var color = Color.Teal;
            const DiscordNotificationType type = DiscordNotificationType.Bug;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithUnicodeCharacters_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor世界";
            const string message = "Test message with unicode: 世界 🌍 🚀";
            var color = Color.Tomato;
            const DiscordNotificationType type = DiscordNotificationType.Holonet;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type));
        }

        [Test]
        public void PublishMessage_WithMultipleFields_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with multiple fields";
            var color = Color.Violet;
            const DiscordNotificationType type = DiscordNotificationType.DMShout;
            const string title = "Test Title";
            var fields = new List<DiscordNotificationField>
            {
                new DiscordNotificationField { Name = "Field1", Value = "Value1", IsInline = true },
                new DiscordNotificationField { Name = "Field2", Value = "Value2", IsInline = false },
                new DiscordNotificationField { Name = "Field3", Value = "Value3", IsInline = true },
                new DiscordNotificationField { Name = "Field4", Value = "Value4", IsInline = false }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title, fields));
        }

        [Test]
        public void PublishMessage_WithFieldContainingSpecialCharacters_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with special field characters";
            var color = Color.Wheat;
            const DiscordNotificationType type = DiscordNotificationType.Bug;
            const string title = "Test Title";
            var fields = new List<DiscordNotificationField>
            {
                new DiscordNotificationField { Name = "Field with spaces", Value = "Value with special chars: !@#$%", IsInline = true },
                new DiscordNotificationField { Name = "Field with unicode", Value = "Value with unicode: 世界 🌍", IsInline = false }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, color, type, title, fields));
        }

        [Test]
        public void PublishMessage_WithDifferentColors_ShouldNotThrow()
        {
            // Arrange
            const string author = "TestAuthor";
            const string message = "Test message with different colors";
            const DiscordNotificationType type = DiscordNotificationType.DMShout;

            // Act & Assert
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, Color.Red, type));
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, Color.Green, type));
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, Color.Blue, type));
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, Color.Transparent, type));
            Assert.DoesNotThrow(() => _discordService.PublishMessage(author, message, Color.FromArgb(128, 64, 192), type));
        }
    }
}
