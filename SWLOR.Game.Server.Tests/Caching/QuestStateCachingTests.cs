using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestStateCacheTests
    {
        private QuestStateCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestStateCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestState()
        {
            // Arrange
            QuestState entity = new QuestState {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestState entity1 = new QuestState { ID = 1};
            QuestState entity2 = new QuestState { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestState entity1 = new QuestState { ID = 1};
            QuestState entity2 = new QuestState { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestState>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestState entity1 = new QuestState { ID = 1};
            QuestState entity2 = new QuestState { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestState>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestState>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestState>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
