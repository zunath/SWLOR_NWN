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
            ClientLogEventType entity = new ClientLogEventType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            ClientLogEventType entity1 = new ClientLogEventType { ID = 1};
            ClientLogEventType entity2 = new ClientLogEventType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            ClientLogEventType entity1 = new ClientLogEventType { ID = 1};
            ClientLogEventType entity2 = new ClientLogEventType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ClientLogEventType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            ClientLogEventType entity1 = new ClientLogEventType { ID = 1};
            ClientLogEventType entity2 = new ClientLogEventType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ClientLogEventType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ClientLogEventType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ClientLogEventType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
