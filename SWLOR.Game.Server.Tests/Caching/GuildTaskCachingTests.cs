using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class GuildTaskCacheTests
    {
        private GuildTaskCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new GuildTaskCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsGuildTask()
        {
            // Arrange
            GuildTask entity = new GuildTask {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            GuildTask entity1 = new GuildTask { ID = 1};
            GuildTask entity2 = new GuildTask { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            GuildTask entity1 = new GuildTask { ID = 1};
            GuildTask entity2 = new GuildTask { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GuildTask>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            GuildTask entity1 = new GuildTask { ID = 1};
            GuildTask entity2 = new GuildTask { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GuildTask>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GuildTask>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GuildTask>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
