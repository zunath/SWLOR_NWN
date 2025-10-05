using NSubstitute;
using SWLOR.Component.Space.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;

namespace SWLOR.Test.Component.Space.Repository
{
    [TestFixture]
    public class PlayerShipRepositoryTests
    {
        private PlayerShipRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new PlayerShipRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnPlayerShip()
        {
            // Arrange
            var shipId = "test-ship-id";
            var expectedShip = new PlayerShip { Id = shipId, OwnerPlayerId = "test-player" };
            _databaseService.Get<PlayerShip>(shipId).Returns(expectedShip);

            // Act
            var result = _repository.GetById(shipId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedShip));
            _databaseService.Received(1).Get<PlayerShip>(shipId);
        }

        [Test]
        public void GetByOwnerPlayerId_WithValidPlayerId_ShouldReturnPlayerShips()
        {
            // Arrange
            var playerId = "test-player-id";
            var expectedShips = new List<PlayerShip>
            {
                new PlayerShip { Id = "ship1", OwnerPlayerId = playerId },
                new PlayerShip { Id = "ship2", OwnerPlayerId = playerId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerShip>>()).Returns(expectedShips);

            // Act
            var result = _repository.GetByOwnerPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedShips));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerShip>>());
        }

        [Test]
        public void GetByPropertyId_WithValidPropertyId_ShouldReturnPlayerShips()
        {
            // Arrange
            var propertyId = "test-property-id";
            var expectedShips = new List<PlayerShip>
            {
                new PlayerShip { Id = "ship1", PropertyId = propertyId },
                new PlayerShip { Id = "ship2", PropertyId = propertyId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerShip>>()).Returns(expectedShips);

            // Act
            var result = _repository.GetByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedShips));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerShip>>());
        }

        [Test]
        public void GetByPropertyIdsExcludingPlayer_WithValidPropertyIds_ShouldReturnFilteredShips()
        {
            // Arrange
            var propertyIds = new List<string> { "prop1", "prop2" };
            var ownerPlayerId = "exclude-player";
            var allShips = new List<PlayerShip>
            {
                new PlayerShip { Id = "ship1", PropertyId = "prop1", OwnerPlayerId = ownerPlayerId },
                new PlayerShip { Id = "ship2", PropertyId = "prop2", OwnerPlayerId = "other-player" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<PlayerShip>>()).Returns(allShips);

            // Act
            var result = _repository.GetByPropertyIdsExcludingPlayer(propertyIds, ownerPlayerId);

            // Assert
            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.First().Id, Is.EqualTo("ship2"));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<PlayerShip>>());
        }

        [Test]
        public void Save_WithValidPlayerShip_ShouldCallDatabaseSet()
        {
            // Arrange
            var playerShip = new PlayerShip { Id = "test-ship", OwnerPlayerId = "test-player" };

            // Act
            _repository.Save(playerShip);

            // Assert
            _databaseService.Received(1).Set(playerShip);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var shipId = "test-ship-id";

            // Act
            _repository.Delete(shipId);

            // Assert
            _databaseService.Received(1).Delete<PlayerShip>(shipId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var shipId = "test-ship-id";
            _databaseService.Exists<PlayerShip>(shipId).Returns(true);

            // Act
            var result = _repository.Exists(shipId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<PlayerShip>(shipId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var shipId = "non-existent-ship";
            _databaseService.Exists<PlayerShip>(shipId).Returns(false);

            // Act
            var result = _repository.Exists(shipId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<PlayerShip>(shipId);
        }
    }
}
