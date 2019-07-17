using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class CustomEffectCategoryCacheTests
    {
        private CustomEffectCategoryCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CustomEffectCategoryCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsCustomEffectCategory()
        {
            // Arrange
            CustomEffectCategory entity = new CustomEffectCategory {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            CustomEffectCategory entity1 = new CustomEffectCategory { ID = 1};
            CustomEffectCategory entity2 = new CustomEffectCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            CustomEffectCategory entity1 = new CustomEffectCategory { ID = 1};
            CustomEffectCategory entity2 = new CustomEffectCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CustomEffectCategory>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            CustomEffectCategory entity1 = new CustomEffectCategory { ID = 1};
            CustomEffectCategory entity2 = new CustomEffectCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CustomEffectCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CustomEffectCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CustomEffectCategory>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
