using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.Inventory.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Test.Component.Inventory.Repository
{
    [TestFixture]
    public class InventoryItemRepositoryTests
    {
        private InventoryItemRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new InventoryItemRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnInventoryItem()
        {
            // Arrange
            var itemId = "test-item-id";
            var expectedItem = new InventoryItem { Id = itemId, Name = "Test Item", Quantity = 1 };
            _databaseService.Get<InventoryItem>(itemId).Returns(expectedItem);

            // Act
            var result = _repository.GetById(itemId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedItem));
            _databaseService.Received(1).Get<InventoryItem>(itemId);
        }

        [Test]
        public void GetByStorageId_WithValidStorageId_ShouldReturnInventoryItems()
        {
            // Arrange
            var storageId = "test-storage-id";
            var expectedItems = new List<InventoryItem>
            {
                new InventoryItem { Id = "item1", StorageId = storageId, Name = "Item 1" },
                new InventoryItem { Id = "item2", StorageId = storageId, Name = "Item 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<InventoryItem>>()).Returns(expectedItems);

            // Act
            var result = _repository.GetByStorageId(storageId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedItems));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<InventoryItem>>());
        }

        [Test]
        public void GetByPlayerId_WithValidPlayerId_ShouldReturnInventoryItems()
        {
            // Arrange
            var playerId = "test-player-id";
            var expectedItems = new List<InventoryItem>
            {
                new InventoryItem { Id = "item1", PlayerId = playerId, Name = "Item 1" },
                new InventoryItem { Id = "item2", PlayerId = playerId, Name = "Item 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<InventoryItem>>()).Returns(expectedItems);

            // Act
            var result = _repository.GetByPlayerId(playerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedItems));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<InventoryItem>>());
        }

        [Test]
        public void Save_WithValidInventoryItem_ShouldCallDatabaseSet()
        {
            // Arrange
            var inventoryItem = new InventoryItem { Id = "test-item", Name = "Test Item", Quantity = 1 };

            // Act
            _repository.Save(inventoryItem);

            // Assert
            _databaseService.Received(1).Set(inventoryItem);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var itemId = "test-item-id";

            // Act
            _repository.Delete(itemId);

            // Assert
            _databaseService.Received(1).Delete<InventoryItem>(itemId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var itemId = "test-item-id";
            _databaseService.Exists<InventoryItem>(itemId).Returns(true);

            // Act
            var result = _repository.Exists(itemId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<InventoryItem>(itemId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var itemId = "non-existent-item";
            _databaseService.Exists<InventoryItem>(itemId).Returns(false);

            // Act
            var result = _repository.Exists(itemId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<InventoryItem>(itemId);
        }
    }
}
