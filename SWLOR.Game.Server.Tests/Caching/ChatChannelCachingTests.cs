using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class ChatChannelCacheTests
    {
        private ChatChannelCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new ChatChannelCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsChatChannel()
        {
            // Arrange
            ChatChannel entity = new ChatChannel {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            ChatChannel entity1 = new ChatChannel { ID = 1};
            ChatChannel entity2 = new ChatChannel { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            ChatChannel entity1 = new ChatChannel { ID = 1};
            ChatChannel entity2 = new ChatChannel { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ChatChannel>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            ChatChannel entity1 = new ChatChannel { ID = 1};
            ChatChannel entity2 = new ChatChannel { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ChatChannel>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ChatChannel>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ChatChannel>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
