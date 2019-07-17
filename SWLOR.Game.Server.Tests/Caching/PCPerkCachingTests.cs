using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCPerkCacheTests
    {
        private PCPerkCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCPerkCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCPerk()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCPerk entity = new PCPerk {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCPerk entity1 = new PCPerk { ID = id1};
            PCPerk entity2 = new PCPerk { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity2));

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
            PCPerk entity1 = new PCPerk { ID = id1};
            PCPerk entity2 = new PCPerk { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCPerk>(entity1));

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
            PCPerk entity1 = new PCPerk { ID = id1};
            PCPerk entity2 = new PCPerk { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCPerk>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCPerk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCPerk>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
