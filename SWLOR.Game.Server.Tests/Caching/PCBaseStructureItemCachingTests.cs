using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCBaseStructureItemCacheTests
    {
        private PCBaseStructureItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCBaseStructureItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCBaseStructureItem()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCBaseStructureItem entity = new PCBaseStructureItem {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBaseStructureItem entity1 = new PCBaseStructureItem { ID = id1};
            PCBaseStructureItem entity2 = new PCBaseStructureItem { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(id1));
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBaseStructureItem entity1 = new PCBaseStructureItem { ID = id1};
            PCBaseStructureItem entity2 = new PCBaseStructureItem { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructureItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBaseStructureItem entity1 = new PCBaseStructureItem { ID = id1};
            PCBaseStructureItem entity2 = new PCBaseStructureItem { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructureItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructureItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructureItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
