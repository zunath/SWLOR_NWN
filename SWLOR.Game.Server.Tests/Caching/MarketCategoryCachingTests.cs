using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class MarketCategoryCacheTests
    {
        private MarketCategoryCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new MarketCategoryCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsMarketCategory()
        {
            // Arrange
            MarketCategory entity = new MarketCategory {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            MarketCategory entity1 = new MarketCategory { ID = 1};
            MarketCategory entity2 = new MarketCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            MarketCategory entity1 = new MarketCategory { ID = 1};
            MarketCategory entity2 = new MarketCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<MarketCategory>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            MarketCategory entity1 = new MarketCategory { ID = 1};
            MarketCategory entity2 = new MarketCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<MarketCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<MarketCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<MarketCategory>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
