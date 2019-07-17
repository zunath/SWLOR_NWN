using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class ComponentTypeCacheTests
    {
        private ComponentTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new ComponentTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsComponentType()
        {
            // Arrange
            ComponentType entity = new ComponentType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            ComponentType entity1 = new ComponentType { ID = 1};
            ComponentType entity2 = new ComponentType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            ComponentType entity1 = new ComponentType { ID = 1};
            ComponentType entity2 = new ComponentType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ComponentType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            ComponentType entity1 = new ComponentType { ID = 1};
            ComponentType entity2 = new ComponentType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ComponentType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ComponentType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ComponentType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
