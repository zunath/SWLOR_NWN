using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCSearchSiteItemCacheTests
    {
        private PCSearchSiteItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCSearchSiteItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCSearchSiteItem()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCSearchSiteItem entity = new PCSearchSiteItem {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity));

            // Assert
            Assert.AreSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCSearchSiteItem entity1 = new PCSearchSiteItem { ID = id1};
            PCSearchSiteItem entity2 = new PCSearchSiteItem { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity2));

            // Assert
            Assert.AreSame(entity1, _cache.GetByID(id1));
            Assert.AreSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCSearchSiteItem entity1 = new PCSearchSiteItem { ID = id1};
            PCSearchSiteItem entity2 = new PCSearchSiteItem { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCSearchSiteItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCSearchSiteItem entity1 = new PCSearchSiteItem { ID = id1};
            PCSearchSiteItem entity2 = new PCSearchSiteItem { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSearchSiteItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCSearchSiteItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCSearchSiteItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
