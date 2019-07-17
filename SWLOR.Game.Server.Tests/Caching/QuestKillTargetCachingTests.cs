using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestKillTargetCacheTests
    {
        private QuestKillTargetCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestKillTargetCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestKillTarget()
        {
            // Arrange
            QuestKillTarget entity = new QuestKillTarget {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestKillTarget entity1 = new QuestKillTarget { ID = 1};
            QuestKillTarget entity2 = new QuestKillTarget { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestKillTarget entity1 = new QuestKillTarget { ID = 1};
            QuestKillTarget entity2 = new QuestKillTarget { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestKillTarget>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestKillTarget entity1 = new QuestKillTarget { ID = 1};
            QuestKillTarget entity2 = new QuestKillTarget { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestKillTarget>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestKillTarget>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestKillTarget>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
