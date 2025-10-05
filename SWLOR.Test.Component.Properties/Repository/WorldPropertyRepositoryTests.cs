using NSubstitute;
using SWLOR.Component.Properties.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;

namespace SWLOR.Test.Component.Properties.Repository
{
    [TestFixture]
    public class WorldPropertyRepositoryTests
    {
        private WorldPropertyRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new WorldPropertyRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnWorldProperty()
        {
            // Arrange
            var propertyId = "test-property-id";
            var expectedProperty = new WorldProperty { Id = propertyId, OwnerPlayerId = "test-player" };
            _databaseService.Get<WorldProperty>(propertyId).Returns(expectedProperty);

            // Act
            var result = _repository.GetById(propertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperty));
            _databaseService.Received(1).Get<WorldProperty>(propertyId);
        }

        [Test]
        public void GetByOwnerPlayerId_WithValidPlayerId_ShouldReturnWorldProperties()
        {
            // Arrange
            var playerId = "test-player-id";
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1", OwnerPlayerId = playerId },
                new WorldProperty { Id = "prop2", OwnerPlayerId = playerId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetByOwnerPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetByPropertyType_WithValidPropertyType_ShouldReturnWorldProperties()
        {
            // Arrange
            var propertyType = PropertyType.Apartment;
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1", PropertyType = propertyType },
                new WorldProperty { Id = "prop2", PropertyType = propertyType }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetByPropertyType(propertyType);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetByParentPropertyId_WithValidParentPropertyId_ShouldReturnWorldProperties()
        {
            // Arrange
            var parentPropertyId = "parent-property-id";
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1", ParentPropertyId = parentPropertyId },
                new WorldProperty { Id = "prop2", ParentPropertyId = parentPropertyId }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetByParentPropertyId(parentPropertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetPubliclyAccessible_ShouldReturnWorldProperties()
        {
            // Arrange
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1", IsPubliclyAccessible = true },
                new WorldProperty { Id = "prop2", IsPubliclyAccessible = true }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetPubliclyAccessible();

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetByStructureType_WithValidStructureType_ShouldReturnWorldProperties()
        {
            // Arrange
            var structureType = StructureType.ObeliskLarge;
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1", StructureType = structureType },
                new WorldProperty { Id = "prop2", StructureType = structureType }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetByStructureType(structureType);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetQueuedForDeletion_ShouldReturnWorldProperties()
        {
            // Arrange
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1", IsQueuedForDeletion = true },
                new WorldProperty { Id = "prop2", IsQueuedForDeletion = true }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetQueuedForDeletion();

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void Save_WithValidWorldProperty_ShouldCallDatabaseSet()
        {
            // Arrange
            var worldProperty = new WorldProperty { Id = "test-property", OwnerPlayerId = "test-player" };

            // Act
            _repository.Save(worldProperty);

            // Assert
            _databaseService.Received(1).Set(worldProperty);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var propertyId = "test-property-id";

            // Act
            _repository.Delete(propertyId);

            // Assert
            _databaseService.Received(1).Delete<WorldProperty>(propertyId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var propertyId = "test-property-id";
            _databaseService.Exists<WorldProperty>(propertyId).Returns(true);

            // Act
            var result = _repository.Exists(propertyId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<WorldProperty>(propertyId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var propertyId = "non-existent-property";
            _databaseService.Exists<WorldProperty>(propertyId).Returns(false);

            // Act
            var result = _repository.Exists(propertyId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<WorldProperty>(propertyId);
        }

        [Test]
        public void GetByPropertyIds_WithValidPropertyIds_ShouldReturnWorldProperties()
        {
            // Arrange
            var propertyIds = new List<string> { "prop1", "prop2", "prop3" };
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1" },
                new WorldProperty { Id = "prop2" },
                new WorldProperty { Id = "prop3" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetByPropertyIds(propertyIds);

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetCountByPropertyType_WithValidPropertyType_ShouldReturnCount()
        {
            // Arrange
            var propertyType = PropertyType.Apartment;
            var expectedCount = 5L;
            _databaseService.SearchCount(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedCount);

            // Act
            var result = _repository.GetCountByPropertyType(propertyType);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _databaseService.Received(1).SearchCount(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetPropertiesWithLeases_ShouldReturnPropertiesWithLeases()
        {
            // Arrange
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "apartment1", PropertyType = PropertyType.Apartment },
                new WorldProperty { Id = "apartment2", PropertyType = PropertyType.Apartment }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetPropertiesWithLeases();

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetActiveCities_ShouldReturnCitiesNotQueuedForDeletion()
        {
            // Arrange
            var expectedCities = new List<WorldProperty>
            {
                new WorldProperty { Id = "city1", PropertyType = PropertyType.City, IsQueuedForDeletion = false },
                new WorldProperty { Id = "city2", PropertyType = PropertyType.City, IsQueuedForDeletion = false }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedCities);

            // Act
            var result = _repository.GetActiveCities();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCities));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetStructuresByParentPropertyId_WithValidParentPropertyId_ShouldReturnStructures()
        {
            // Arrange
            var parentPropertyId = "parent-property-id";
            var expectedStructures = new List<WorldProperty>
            {
                new WorldProperty { Id = "structure1", ParentPropertyId = parentPropertyId, PropertyType = PropertyType.Structure },
                new WorldProperty { Id = "structure2", ParentPropertyId = parentPropertyId, PropertyType = PropertyType.Structure }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedStructures);

            // Act
            var result = _repository.GetStructuresByParentPropertyId(parentPropertyId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedStructures));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetActiveStructureCountByParentAndType_WithValidParameters_ShouldReturnCount()
        {
            // Arrange
            var parentPropertyId = "parent-property-id";
            var structureType = StructureType.ObeliskLarge;
            var expectedCount = 3L;
            _databaseService.SearchCount(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedCount);

            // Act
            var result = _repository.GetActiveStructureCountByParentAndType(parentPropertyId, structureType);

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _databaseService.Received(1).SearchCount(Arg.Any<IDBQuery<WorldProperty>>());
        }

        [Test]
        public void GetAll_ShouldReturnAllWorldProperties()
        {
            // Arrange
            var expectedProperties = new List<WorldProperty>
            {
                new WorldProperty { Id = "prop1" },
                new WorldProperty { Id = "prop2" },
                new WorldProperty { Id = "prop3" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<WorldProperty>>()).Returns(expectedProperties);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedProperties));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<WorldProperty>>());
        }
    }
}
