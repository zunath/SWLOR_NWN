using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestCacheTests
    {
        private QuestCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuest()
        {
            // Arrange
            Quest entity = new Quest {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Quest entity1 = new Quest { ID = 1};
            Quest entity2 = new Quest { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Quest entity1 = new Quest { ID = 1};
            Quest entity2 = new Quest { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Quest>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Quest entity1 = new Quest { ID = 1};
            Quest entity2 = new Quest { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Quest>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Quest>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Quest>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
