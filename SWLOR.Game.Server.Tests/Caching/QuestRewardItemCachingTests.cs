using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class QuestRewardItemCacheTests
    {
        private QuestRewardItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new QuestRewardItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsQuestRewardItem()
        {
            // Arrange
            QuestRewardItem entity = new QuestRewardItem {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            QuestRewardItem entity1 = new QuestRewardItem { ID = 1};
            QuestRewardItem entity2 = new QuestRewardItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            QuestRewardItem entity1 = new QuestRewardItem { ID = 1};
            QuestRewardItem entity2 = new QuestRewardItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRewardItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            QuestRewardItem entity1 = new QuestRewardItem { ID = 1};
            QuestRewardItem entity2 = new QuestRewardItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<QuestRewardItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRewardItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<QuestRewardItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
