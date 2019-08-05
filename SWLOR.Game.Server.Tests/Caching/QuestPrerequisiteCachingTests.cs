using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestPrerequisiteCacheTests
    {
        private QuestPrerequisiteCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestPrerequisiteCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestPrerequisite()
        {
            // Arrange
            QuestPrerequisite entity = new QuestPrerequisite {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestPrerequisite entity1 = new QuestPrerequisite { ID = 1};
            QuestPrerequisite entity2 = new QuestPrerequisite { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestPrerequisite entity1 = new QuestPrerequisite { ID = 1};
            QuestPrerequisite entity2 = new QuestPrerequisite { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestPrerequisite>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestPrerequisite entity1 = new QuestPrerequisite { ID = 1};
            QuestPrerequisite entity2 = new QuestPrerequisite { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestPrerequisite>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestPrerequisite>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestPrerequisite>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
