using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class LootTableCacheTests
    {
        private LootTableCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new LootTableCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsLootTable()
        {
            // Arrange
            LootTable entity = new LootTable {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            LootTable entity1 = new LootTable { ID = 1};
            LootTable entity2 = new LootTable { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            LootTable entity1 = new LootTable { ID = 1};
            LootTable entity2 = new LootTable { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<LootTable>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            LootTable entity1 = new LootTable { ID = 1};
            LootTable entity2 = new LootTable { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTable>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<LootTable>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<LootTable>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
