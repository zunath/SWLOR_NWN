using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestRequiredItemCacheTests
    {
        private QuestRequiredItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestRequiredItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestRequiredItem()
        {
            // Arrange
            QuestRequiredItem entity = new QuestRequiredItem {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestRequiredItem entity1 = new QuestRequiredItem { ID = 1};
            QuestRequiredItem entity2 = new QuestRequiredItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestRequiredItem entity1 = new QuestRequiredItem { ID = 1};
            QuestRequiredItem entity2 = new QuestRequiredItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRequiredItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestRequiredItem entity1 = new QuestRequiredItem { ID = 1};
            QuestRequiredItem entity2 = new QuestRequiredItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRequiredItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRequiredItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
