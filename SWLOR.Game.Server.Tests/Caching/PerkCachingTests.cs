using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PerkCacheTests
    {
        private PerkCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PerkCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPerk()
        {
            // Arrange
            Data.Entity.Perk entity = new Data.Entity.Perk {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Data.Entity.Perk entity1 = new Data.Entity.Perk { ID = 1};
            Data.Entity.Perk entity2 = new Data.Entity.Perk { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Data.Entity.Perk entity1 = new Data.Entity.Perk { ID = 1};
            Data.Entity.Perk entity2 = new Data.Entity.Perk { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Data.Entity.Perk>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Data.Entity.Perk entity1 = new Data.Entity.Perk { ID = 1};
            Data.Entity.Perk entity2 = new Data.Entity.Perk { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.Perk>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Data.Entity.Perk>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Data.Entity.Perk>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
