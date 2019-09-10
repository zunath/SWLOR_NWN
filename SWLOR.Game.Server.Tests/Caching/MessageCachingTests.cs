using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class MessageCacheTests
    {
        private MessageCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new MessageCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsMessage()
        {
            // Arrange
            var id = Guid.NewGuid();
            Message entity = new Message {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            Message entity1 = new Message { ID = id1};
            Message entity2 = new Message { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity2));

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
            Message entity1 = new Message { ID = id1};
            Message entity2 = new Message { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Message>(entity1));

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
            Message entity1 = new Message { ID = id1};
            Message entity2 = new Message { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Message>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Message>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Message>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
