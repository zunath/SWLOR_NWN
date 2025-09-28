using NSubstitute;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.Communication.Service
{
    [TestFixture]
    public class DialogServiceTests : TestBase
    {
        private ILogger _mockLogger;
        private IServiceProvider _mockServiceProvider;
        private DialogService _dialogService;

        [SetUp]
        public void SetUp()
        {
            _mockLogger = Substitute.For<ILogger>();
            _mockServiceProvider = Substitute.For<IServiceProvider>();
            _dialogService = new DialogService(_mockLogger, _mockServiceProvider);
        }

        [Test]
        public void LoadConversation_ShouldThrow_WhenConversationNotRegistered()
        {
            // Arrange
            var player = 123u;
            var talkTo = 456u;
            var className = "NonExistentDialog";

            // Act & Assert - Should throw when conversation doesn't exist
            Assert.Throws<KeyNotFoundException>(() => 
                _dialogService.LoadConversation(player, talkTo, className, -1));
        }

        [Test]
        public void LoadConversation_ShouldThrow_WhenClassNameIsNullOrEmpty()
        {
            // Arrange
            var player = 123u;
            var talkTo = 456u;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                _dialogService.LoadConversation(player, talkTo, null, -1));
            
            Assert.Throws<ArgumentException>(() => 
                _dialogService.LoadConversation(player, talkTo, "", -1));
            
            Assert.Throws<ArgumentException>(() => 
                _dialogService.LoadConversation(player, talkTo, "   ", -1));
        }

        [Test]
        public void LoadConversation_ShouldThrow_WhenDialogNumberIsOutOfRange()
        {
            // Arrange
            var player = 123u;
            var talkTo = 456u;
            var className = "TestDialog";

            // Act & Assert - Should throw for invalid dialog numbers
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                _dialogService.LoadConversation(player, talkTo, className, 0));
            
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                _dialogService.LoadConversation(player, talkTo, className, 1000));
        }

        [Test]
        public void HasPlayerDialog_ShouldReturnFalse_WhenNoDialogExists()
        {
            // Arrange
            var playerId = "non-existent-player";

            // Act
            var result = _dialogService.HasPlayerDialog(playerId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void LoadPlayerDialog_ShouldThrow_WhenDialogDoesNotExist()
        {
            // Arrange
            var playerId = "non-existent-player";

            // Act & Assert
            Assert.Throws<Exception>(() => 
                _dialogService.LoadPlayerDialog(playerId));
        }

        [Test]
        public void GetConversation_ShouldThrow_WhenConversationNotRegistered()
        {
            // Arrange
            var className = "NonExistentDialog";

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => 
                _dialogService.GetConversation(className));
        }
    }
}