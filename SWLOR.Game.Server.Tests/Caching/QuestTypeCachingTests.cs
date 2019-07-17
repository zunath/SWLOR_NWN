using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestTypeCacheTests
    {
        private QuestTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestType()
        {
            // Arrange
            QuestType entity = new QuestType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestType entity1 = new QuestType { ID = 1};
            QuestType entity2 = new QuestType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestType entity1 = new QuestType { ID = 1};
            QuestType entity2 = new QuestType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestType entity1 = new QuestType { ID = 1};
            QuestType entity2 = new QuestType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
