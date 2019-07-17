using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestRequiredKeyItemCacheTests
    {
        private QuestRequiredKeyItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestRequiredKeyItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestRequiredKeyItem()
        {
            // Arrange
            QuestRequiredKeyItem entity = new QuestRequiredKeyItem {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestRequiredKeyItem entity1 = new QuestRequiredKeyItem { ID = 1};
            QuestRequiredKeyItem entity2 = new QuestRequiredKeyItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestRequiredKeyItem entity1 = new QuestRequiredKeyItem { ID = 1};
            QuestRequiredKeyItem entity2 = new QuestRequiredKeyItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRequiredKeyItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestRequiredKeyItem entity1 = new QuestRequiredKeyItem { ID = 1};
            QuestRequiredKeyItem entity2 = new QuestRequiredKeyItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRequiredKeyItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRequiredKeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRequiredKeyItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
