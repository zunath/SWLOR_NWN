using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class CustomEffectCacheTests
    {
        private CustomEffectCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CustomEffectCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsCustomEffect()
        {
            // Arrange
            Data.Entity.CustomEffect entity = new Data.Entity.CustomEffect {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Data.Entity.CustomEffect entity1 = new Data.Entity.CustomEffect { ID = 1};
            Data.Entity.CustomEffect entity2 = new Data.Entity.CustomEffect { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Data.Entity.CustomEffect entity1 = new Data.Entity.CustomEffect { ID = 1};
            Data.Entity.CustomEffect entity2 = new Data.Entity.CustomEffect { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Data.Entity.CustomEffect>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Data.Entity.CustomEffect entity1 = new Data.Entity.CustomEffect { ID = 1};
            Data.Entity.CustomEffect entity2 = new Data.Entity.CustomEffect { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Data.Entity.CustomEffect>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Data.Entity.CustomEffect>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Data.Entity.CustomEffect>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
