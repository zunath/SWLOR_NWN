using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class LootTableItemCacheTests
    {
        private LootTableItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new LootTableItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsLootTableItem()
        {
            // Arrange
            LootTableItem entity = new LootTableItem {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            LootTableItem entity1 = new LootTableItem { ID = 1};
            LootTableItem entity2 = new LootTableItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            LootTableItem entity1 = new LootTableItem { ID = 1};
            LootTableItem entity2 = new LootTableItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<LootTableItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            LootTableItem entity1 = new LootTableItem { ID = 1};
            LootTableItem entity2 = new LootTableItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<LootTableItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<LootTableItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<LootTableItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
