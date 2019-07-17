using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class GuildCacheTests
    {
        private GuildCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new GuildCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsGuild()
        {
            // Arrange
            Guild entity = new Guild {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Guild entity1 = new Guild { ID = 1};
            Guild entity2 = new Guild { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Guild entity1 = new Guild { ID = 1};
            Guild entity2 = new Guild { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Guild>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Guild entity1 = new Guild { ID = 1};
            Guild entity2 = new Guild { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Guild>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Guild>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Guild>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
