using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.Character.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.Character.Enums;

namespace SWLOR.Test.Component.Character.Repository
{
    [TestFixture]
    public class PlayerRepositoryTests
    {
        private PlayerRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new PlayerRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnPlayer()
        {
            // Arrange
            var playerId = "test-player-id";
            var expectedPlayer = new Player { Id = playerId, Name = "Test Player" };
            _databaseService.Get<Player>(playerId).Returns(expectedPlayer);

            // Act
            var result = _repository.GetById(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayer));
            _databaseService.Received(1).Get<Player>(playerId);
        }

        [Test]
        public void GetByAreaResref_WithValidAreaResref_ShouldReturnPlayers()
        {
            // Arrange
            var areaResref = "test-area";
            var expectedPlayers = new List<Player>
            {
                new Player { Id = "player1", LocationAreaResref = areaResref },
                new Player { Id = "player2", LocationAreaResref = areaResref }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Player>>()).Returns(expectedPlayers);

            // Act
            var result = _repository.GetByAreaResref(areaResref);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayers));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Player>>());
        }

        [Test]
        public void GetByCharacterType_WithValidCharacterType_ShouldReturnPlayers()
        {
            // Arrange
            var characterType = CharacterType.Standard;
            var expectedPlayers = new List<Player>
            {
                new Player { Id = "player1", CharacterType = characterType },
                new Player { Id = "player2", CharacterType = characterType }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Player>>()).Returns(expectedPlayers);

            // Act
            var result = _repository.GetByCharacterType(characterType);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayers));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Player>>());
        }

        [Test]
        public void Save_WithValidPlayer_ShouldCallDatabaseSet()
        {
            // Arrange
            var player = new Player { Id = "test-player", Name = "Test Player" };

            // Act
            _repository.Save(player);

            // Assert
            _databaseService.Received(1).Set(player);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var playerId = "test-player-id";

            // Act
            _repository.Delete(playerId);

            // Assert
            _databaseService.Received(1).Delete<Player>(playerId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var playerId = "test-player-id";
            _databaseService.Exists<Player>(playerId).Returns(true);

            // Act
            var result = _repository.Exists(playerId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<Player>(playerId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var playerId = "non-existent-player";
            _databaseService.Exists<Player>(playerId).Returns(false);

            // Act
            var result = _repository.Exists(playerId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<Player>(playerId);
        }

        [Test]
        public void GetByCitizenPropertyId_WithValidCitizenPropertyId_ShouldReturnPlayers()
        {
            // Arrange
            var citizenPropertyId = "test-citizen-property-id";
            var expectedPlayers = new List<Player>
            {
                new Player { Id = "player1", CitizenPropertyId = citizenPropertyId },
                new Player { Id = "player2", CitizenPropertyId = citizenPropertyId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Player>>()).Returns(expectedPlayers);

            // Act
            var result = _repository.GetByCitizenPropertyId(citizenPropertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayers));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Player>>());
        }

        [Test]
        public void GetActivePlayers_ShouldReturnNonDeletedPlayers()
        {
            // Arrange
            var expectedPlayers = new List<Player>
            {
                new Player { Id = "player1", IsDeleted = false },
                new Player { Id = "player2", IsDeleted = false }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Player>>()).Returns(expectedPlayers);

            // Act
            var result = _repository.GetActivePlayers();

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayers));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Player>>());
        }

        [Test]
        public void GetActiveCitizensByPropertyId_WithValidCitizenPropertyId_ShouldReturnActiveCitizens()
        {
            // Arrange
            var citizenPropertyId = "test-citizen-property-id";
            var expectedPlayers = new List<Player>
            {
                new Player { Id = "player1", CitizenPropertyId = citizenPropertyId, IsDeleted = false },
                new Player { Id = "player2", CitizenPropertyId = citizenPropertyId, IsDeleted = false }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Player>>()).Returns(expectedPlayers);

            // Act
            var result = _repository.GetActiveCitizensByPropertyId(citizenPropertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayers));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Player>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllPlayers()
        {
            // Arrange
            var expectedPlayers = new List<Player>
            {
                new Player { Id = "player1" },
                new Player { Id = "player2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<Player>>()).Returns(expectedPlayers);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlayers));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<Player>>());
        }

        [Test]
        public void GetCount_ShouldReturnCount()
        {
            // Arrange
            var expectedCount = 5L;
            _databaseService.SearchCount(Arg.Any<IDBQuery<Player>>()).Returns(expectedCount);

            // Act
            var result = _repository.GetCount();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _databaseService.Received(1).SearchCount(Arg.Any<IDBQuery<Player>>());
        }
    }
}
