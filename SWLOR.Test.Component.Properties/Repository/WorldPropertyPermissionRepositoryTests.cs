using NSubstitute;
using SWLOR.Component.Properties.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Properties.Entities;

namespace SWLOR.Test.Component.Properties.Repository
{
    [TestFixture]
    public class WorldPropertyPermissionRepositoryTests
    {
        private WorldPropertyPermissionRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new WorldPropertyPermissionRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnWorldPropertyPermission()
        {
            // Arrange
            var permissionId = "test-permission-id";
            var expectedPermission = new WorldPropertyPermission { Id = permissionId, PropertyId = "test-property", PlayerId = "test-player" };
            _databaseService.Get<WorldPropertyPermission>(permissionId).Returns(expectedPermission);

            // Act
            var result = _repository.GetById(permissionId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermission));
            _databaseService.Received(1).Get<WorldPropertyPermission>(permissionId);
        }

        [Test]
        public void GetByPropertyId_WithValidPropertyId_ShouldReturnWorldPropertyPermissions()
        {
            // Arrange
            var propertyId = "test-property-id";
            var expectedPermissions = new List<WorldPropertyPermission>
            {
                new WorldPropertyPermission { Id = "perm1", PropertyId = propertyId, PlayerId = "player1" },
                new WorldPropertyPermission { Id = "perm2", PropertyId = propertyId, PlayerId = "player2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(expectedPermissions);

            // Act
            var result = _repository.GetByPropertyId(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermissions));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void GetByPlayerId_WithValidPlayerId_ShouldReturnWorldPropertyPermissions()
        {
            // Arrange
            var playerId = "test-player-id";
            var expectedPermissions = new List<WorldPropertyPermission>
            {
                new WorldPropertyPermission { Id = "perm1", PropertyId = "property1", PlayerId = playerId },
                new WorldPropertyPermission { Id = "perm2", PropertyId = "property2", PlayerId = playerId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(expectedPermissions);

            // Act
            var result = _repository.GetByPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermissions));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void GetByPropertyIdAndPlayerId_WithValidIds_ShouldReturnWorldPropertyPermissions()
        {
            // Arrange
            var propertyId = "test-property-id";
            var playerId = "test-player-id";
            var expectedPermissions = new List<WorldPropertyPermission> { new WorldPropertyPermission { Id = "perm1", PropertyId = propertyId, PlayerId = playerId } };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(expectedPermissions);

            // Act
            var result = _repository.GetByPropertyIdAndPlayerId(propertyId, playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermissions));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void GetByPropertyIdAndPlayerId_WithNoMatchingPermission_ShouldReturnEmptyCollection()
        {
            // Arrange
            var propertyId = "test-property-id";
            var playerId = "test-player-id";
            var searchResults = new List<WorldPropertyPermission>();
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(searchResults);

            // Act
            var result = _repository.GetByPropertyIdAndPlayerId(propertyId, playerId);

            // Assert
            Assert.That(result, Is.EqualTo(searchResults));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void Save_WithValidWorldPropertyPermission_ShouldCallDatabaseSet()
        {
            // Arrange
            var permission = new WorldPropertyPermission { Id = "test-permission", PropertyId = "test-property", PlayerId = "test-player" };

            // Act
            _repository.Save(permission);

            // Assert
            _databaseService.Received(1).Set(permission);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var permissionId = "test-permission-id";

            // Act
            _repository.Delete(permissionId);

            // Assert
            _databaseService.Received(1).Delete<WorldPropertyPermission>(permissionId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var permissionId = "test-permission-id";
            _databaseService.Exists<WorldPropertyPermission>(permissionId).Returns(true);

            // Act
            var result = _repository.Exists(permissionId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<WorldPropertyPermission>(permissionId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var permissionId = "non-existent-permission";
            _databaseService.Exists<WorldPropertyPermission>(permissionId).Returns(false);

            // Act
            var result = _repository.Exists(permissionId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<WorldPropertyPermission>(permissionId);
        }

        [Test]
        public void GetSingleByPropertyIdAndPlayerId_WithValidIds_ShouldReturnSinglePermission()
        {
            // Arrange
            var propertyId = "test-property-id";
            var playerId = "test-player-id";
            var expectedPermission = new WorldPropertyPermission { Id = "perm1", PropertyId = propertyId, PlayerId = playerId };
            var searchResults = new List<WorldPropertyPermission> { expectedPermission };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(searchResults);

            // Act
            var result = _repository.GetSingleByPropertyIdAndPlayerId(propertyId, playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermission));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void GetSingleByPropertyIdAndPlayerId_WithNoMatchingPermission_ShouldReturnNull()
        {
            // Arrange
            var propertyId = "test-property-id";
            var playerId = "test-player-id";
            var searchResults = new List<WorldPropertyPermission>();
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(searchResults);

            // Act
            var result = _repository.GetSingleByPropertyIdAndPlayerId(propertyId, playerId);

            // Assert
            Assert.That(result, Is.Null);
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void GetByPropertyIds_WithValidPropertyIds_ShouldReturnWorldPropertyPermissions()
        {
            // Arrange
            var propertyIds = new List<string> { "prop1", "prop2", "prop3" };
            var expectedPermissions = new List<WorldPropertyPermission>
            {
                new WorldPropertyPermission { Id = "perm1", PropertyId = "prop1" },
                new WorldPropertyPermission { Id = "perm2", PropertyId = "prop2" },
                new WorldPropertyPermission { Id = "perm3", PropertyId = "prop3" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(expectedPermissions);

            // Act
            var result = _repository.GetByPropertyIds(propertyIds);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermissions));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }

        [Test]
        public void GetByPlayerIdAndPropertyIds_WithValidPlayerIdAndPropertyIds_ShouldReturnWorldPropertyPermissions()
        {
            // Arrange
            var playerId = "test-player-id";
            var propertyIds = new List<string> { "prop1", "prop2", "prop3" };
            var expectedPermissions = new List<WorldPropertyPermission>
            {
                new WorldPropertyPermission { Id = "perm1", PropertyId = "prop1", PlayerId = playerId },
                new WorldPropertyPermission { Id = "perm2", PropertyId = "prop2", PlayerId = playerId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldPropertyPermission>>()).Returns(expectedPermissions);

            // Act
            var result = _repository.GetByPlayerIdAndPropertyIds(playerId, propertyIds);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPermissions));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldPropertyPermission>>());
        }
    }
}
