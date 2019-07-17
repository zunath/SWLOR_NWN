using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PerkLevelCacheTests
    {
        private PerkLevelCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PerkLevelCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPerkLevel()
        {
            // Arrange
            PerkLevel entity = new PerkLevel {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            PerkLevel entity1 = new PerkLevel { ID = 1};
            PerkLevel entity2 = new PerkLevel { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            PerkLevel entity1 = new PerkLevel { ID = 1};
            PerkLevel entity2 = new PerkLevel { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevel>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            PerkLevel entity1 = new PerkLevel { ID = 1};
            PerkLevel entity2 = new PerkLevel { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevel>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevel>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
