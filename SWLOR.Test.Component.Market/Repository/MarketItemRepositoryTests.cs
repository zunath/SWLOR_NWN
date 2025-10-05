using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.Market.Repository;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Test.Component.Market.Repository
{
    [TestFixture]
    public class MarketItemRepositoryTests
    {
        private MarketItemRepository _repository;
        private IDatabaseService _databaseService;

        [SetUp]
        public void SetUp()
        {
            _databaseService = Substitute.For<IDatabaseService>();
            _repository = new MarketItemRepository(_databaseService);
        }

        [Test]
        public void GetById_WithValidId_ShouldReturnMarketItem()
        {
            // Arrange
            var itemId = "test-item-id";
            var expectedItem = new MarketItem { Id = itemId, Name = "Test Item", Price = 100 };
            _databaseService.Get<MarketItem>(itemId).Returns(expectedItem);

            // Act
            var result = _repository.GetById(itemId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedItem));
            _databaseService.Received(1).Get<MarketItem>(itemId);
        }

        [Test]
        public void GetBySellerPlayerId_WithValidPlayerId_ShouldReturnMarketItems()
        {
            // Arrange
            var sellerPlayerId = "test-seller-id";
            var expectedItems = new List<MarketItem>
            {
                new MarketItem { Id = "item1", PlayerId = sellerPlayerId, Name = "Item 1" },
                new MarketItem { Id = "item2", PlayerId = sellerPlayerId, Name = "Item 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<MarketItem>>()).Returns(expectedItems);

            // Act
            var result = _repository.GetBySellerPlayerId(sellerPlayerId);

            // Assert
            Assert.That(result, Is.EqualTo(expectedItems));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<MarketItem>>());
        }

        [Test]
        public void GetByItemResref_WithValidItemResref_ShouldReturnMarketItems()
        {
            // Arrange
            var itemResref = "test-item-resref";
            var expectedItems = new List<MarketItem>
            {
                new MarketItem { Id = "item1", Resref = itemResref, Name = "Item 1" },
                new MarketItem { Id = "item2", Resref = itemResref, Name = "Item 2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<MarketItem>>()).Returns(expectedItems);

            // Act
            var result = _repository.GetByItemResref(itemResref);

            // Assert
            Assert.That(result, Is.EqualTo(expectedItems));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<MarketItem>>());
        }

        [Test]
        public void Save_WithValidMarketItem_ShouldCallDatabaseSet()
        {
            // Arrange
            var marketItem = new MarketItem { Id = "test-item", Name = "Test Item", Price = 100 };

            // Act
            _repository.Save(marketItem);

            // Assert
            _databaseService.Received(1).Set(marketItem);
        }

        [Test]
        public void Delete_WithValidId_ShouldCallDatabaseDelete()
        {
            // Arrange
            var itemId = "test-item-id";

            // Act
            _repository.Delete(itemId);

            // Assert
            _databaseService.Received(1).Delete<MarketItem>(itemId);
        }

        [Test]
        public void Exists_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var itemId = "test-item-id";
            _databaseService.Exists<MarketItem>(itemId).Returns(true);

            // Act
            var result = _repository.Exists(itemId);

            // Assert
            Assert.That(result, Is.True);
            _databaseService.Received(1).Exists<MarketItem>(itemId);
        }

        [Test]
        public void Exists_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var itemId = "non-existent-item";
            _databaseService.Exists<MarketItem>(itemId).Returns(false);

            // Act
            var result = _repository.Exists(itemId);

            // Assert
            Assert.That(result, Is.False);
            _databaseService.Received(1).Exists<MarketItem>(itemId);
        }

        [Test]
        public void GetAll_ShouldReturnAllMarketItems()
        {
            // Arrange
            var expectedItems = new List<MarketItem>
            {
                new MarketItem { Id = "item1" },
                new MarketItem { Id = "item2" }
            };
            _databaseService.Search(Arg.Any<IDBQuery<MarketItem>>()).Returns(expectedItems);

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.That(result, Is.EqualTo(expectedItems));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<MarketItem>>());
        }

        [Test]
        public void GetCount_ShouldReturnCount()
        {
            // Arrange
            var expectedCount = 10L;
            _databaseService.SearchCount(Arg.Any<IDBQuery<MarketItem>>()).Returns(expectedCount);

            // Act
            var result = _repository.GetCount();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _databaseService.Received(1).SearchCount(Arg.Any<IDBQuery<MarketItem>>());
        }

        [Test]
        public void GetListedItems_ShouldReturnListedMarketItems()
        {
            // Arrange
            var expectedItems = new List<MarketItem>
            {
                new MarketItem { Id = "item1", IsListed = true },
                new MarketItem { Id = "item2", IsListed = true }
            };
            _databaseService.Search(Arg.Any<IDBQuery<MarketItem>>()).Returns(expectedItems);

            // Act
            var result = _repository.GetListedItems();

            // Assert
            Assert.That(result, Is.EqualTo(expectedItems));
            _databaseService.Received(1).Search(Arg.Any<IDBQuery<MarketItem>>());
        }

        [Test]
        public void GetListedCount_ShouldReturnListedCount()
        {
            // Arrange
            var expectedCount = 5L;
            _databaseService.SearchCount(Arg.Any<IDBQuery<MarketItem>>()).Returns(expectedCount);

            // Act
            var result = _repository.GetListedCount();

            // Assert
            Assert.That(result, Is.EqualTo(expectedCount));
            _databaseService.Received(1).SearchCount(Arg.Any<IDBQuery<MarketItem>>());
        }
    }
}
