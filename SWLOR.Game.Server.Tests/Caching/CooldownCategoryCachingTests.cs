using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class CooldownCategoryCacheTests
    {
        private CooldownCategoryCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CooldownCategoryCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsCooldownCategory()
        {
            // Arrange
            CooldownCategory entity = new CooldownCategory {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            CooldownCategory entity1 = new CooldownCategory { ID = 1};
            CooldownCategory entity2 = new CooldownCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            CooldownCategory entity1 = new CooldownCategory { ID = 1};
            CooldownCategory entity2 = new CooldownCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CooldownCategory>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            CooldownCategory entity1 = new CooldownCategory { ID = 1};
            CooldownCategory entity2 = new CooldownCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CooldownCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CooldownCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CooldownCategory>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
