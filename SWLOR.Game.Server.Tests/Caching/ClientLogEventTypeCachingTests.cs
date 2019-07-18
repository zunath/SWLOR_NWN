using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class ClientLogEventTypeCacheTests
    {
        private ClientLogEventTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new ClientLogEventTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsClientLogEventType()
        {
            // Arrange
            ModuleEventType entity = new ModuleEventType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            ModuleEventType entity1 = new ModuleEventType { ID = 1};
            ModuleEventType entity2 = new ModuleEventType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            ModuleEventType entity1 = new ModuleEventType { ID = 1};
            ModuleEventType entity2 = new ModuleEventType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ModuleEventType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            ModuleEventType entity1 = new ModuleEventType { ID = 1};
            ModuleEventType entity2 = new ModuleEventType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ModuleEventType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ModuleEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ModuleEventType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
