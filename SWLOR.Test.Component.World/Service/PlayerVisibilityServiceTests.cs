using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.World.Service;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.World.Service
{
    [TestFixture]
    public class PlayerVisibilityServiceTests : TestBase
    {
        private ILogger _mockLogger;
        private IDatabaseService _mockDatabaseService;
        private IVisibilityObjectCacheService _mockVisibilityObjectCache;
        private PlayerVisibilityService _service;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service for all tests
            InitializeMockNWScript();
            
            _mockLogger = Substitute.For<ILogger>();
            _mockDatabaseService = Substitute.For<IDatabaseService>();
            _mockVisibilityObjectCache = Substitute.For<IVisibilityObjectCacheService>();
            _service = new PlayerVisibilityService(_mockLogger, _mockDatabaseService, _mockVisibilityObjectCache);
        }

        // Note: LoadPlayerVisibilityObjects is an event handler method with [ScriptHandler<OnModuleEnter>] attribute
        // It's designed to be called by the event system, not directly in unit tests.
        // Testing event handlers requires integration testing rather than unit testing.

        [Test]
        public void GetPlayerObjectVisibility_WithExistingVisibility_ShouldReturnVisibilityType()
        {
            // Arrange
            var playerId = "test-player-123";
            var visibilityObjectId = "test-object-1";
            var expectedVisibilityType = VisibilityType.Hidden;

            var player = new Player(playerId);
            player.ObjectVisibilities[visibilityObjectId] = expectedVisibilityType;

            _mockDatabaseService.Get<Player>(playerId).Returns(player);

            // Act
            var result = _service.GetPlayerObjectVisibility(playerId, visibilityObjectId);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(expectedVisibilityType));
        }

        [Test]
        public void GetPlayerObjectVisibility_WithNonExistentPlayer_ShouldReturnNull()
        {
            // Arrange
            var playerId = "non-existent-player";
            var visibilityObjectId = "test-object-1";

            _mockDatabaseService.Get<Player>(playerId).Returns((Player)null);

            // Act
            var result = _service.GetPlayerObjectVisibility(playerId, visibilityObjectId);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void GetPlayerObjectVisibility_WithNonExistentObject_ShouldReturnNull()
        {
            // Arrange
            var playerId = "test-player-123";
            var visibilityObjectId = "non-existent-object";

            var player = new Player(playerId);
            _mockDatabaseService.Get<Player>(playerId).Returns(player);

            // Act
            var result = _service.GetPlayerObjectVisibility(playerId, visibilityObjectId);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void SetPlayerObjectVisibility_ShouldUpdatePlayerData()
        {
            // Arrange
            var playerId = "test-player-123";
            var visibilityObjectId = "test-object-1";
            var visibilityType = VisibilityType.Hidden;

            var player = new Player(playerId);
            _mockDatabaseService.Get<Player>(playerId).Returns(player);

            // Act
            _service.SetPlayerObjectVisibility(playerId, visibilityObjectId, visibilityType);

            // Assert
            _mockDatabaseService.Received(1).Set(Arg.Any<Player>());
        }

        [Test]
        public void SetPlayerObjectVisibility_WithNewPlayer_ShouldCreatePlayer()
        {
            // Arrange
            var playerId = "new-player-123";
            var visibilityObjectId = "test-object-1";
            var visibilityType = VisibilityType.Hidden;

            _mockDatabaseService.Get<Player>(playerId).Returns((Player)null);

            // Act
            _service.SetPlayerObjectVisibility(playerId, visibilityObjectId, visibilityType);

            // Assert
            _mockDatabaseService.Received(1).Set(Arg.Any<Player>());
        }

        [Test]
        public void RemovePlayerObjectVisibility_WithExistingVisibility_ShouldRemoveFromPlayerData()
        {
            // Arrange
            var playerId = "test-player-123";
            var visibilityObjectId = "test-object-1";

            var player = new Player(playerId);
            player.ObjectVisibilities[visibilityObjectId] = VisibilityType.Hidden;

            _mockDatabaseService.Get<Player>(playerId).Returns(player);

            // Act
            _service.RemovePlayerObjectVisibility(playerId, visibilityObjectId);

            // Assert
            _mockDatabaseService.Received(1).Set(Arg.Any<Player>());
        }

        [Test]
        public void RemovePlayerObjectVisibility_WithNonExistentPlayer_ShouldNotThrow()
        {
            // Arrange
            var playerId = "non-existent-player";
            var visibilityObjectId = "test-object-1";

            _mockDatabaseService.Get<Player>(playerId).Returns((Player)null);

            // Act & Assert
            Assert.DoesNotThrow(() => _service.RemovePlayerObjectVisibility(playerId, visibilityObjectId));
        }
    }
}
