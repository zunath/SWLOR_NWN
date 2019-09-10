using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class ItemTypeCacheTests
    {
        private ItemTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new ItemTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsItemType()
        {
            // Arrange
            ItemType entity = new ItemType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            ItemType entity1 = new ItemType { ID = 1};
            ItemType entity2 = new ItemType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            ItemType entity1 = new ItemType { ID = 1};
            ItemType entity2 = new ItemType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ItemType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            ItemType entity1 = new ItemType { ID = 1};
            ItemType entity2 = new ItemType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ItemType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ItemType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ItemType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
