using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCQuestKillTargetProgressCacheTests
    {
        private PCQuestKillTargetProgressCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCQuestKillTargetProgressCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCQuestKillTargetProgress()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCQuestKillTargetProgress entity = new PCQuestKillTargetProgress {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCQuestKillTargetProgress entity1 = new PCQuestKillTargetProgress { ID = id1};
            PCQuestKillTargetProgress entity2 = new PCQuestKillTargetProgress { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(id1));
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCQuestKillTargetProgress entity1 = new PCQuestKillTargetProgress { ID = id1};
            PCQuestKillTargetProgress entity2 = new PCQuestKillTargetProgress { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCQuestKillTargetProgress>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCQuestKillTargetProgress entity1 = new PCQuestKillTargetProgress { ID = id1};
            PCQuestKillTargetProgress entity2 = new PCQuestKillTargetProgress { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCQuestKillTargetProgress>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCQuestKillTargetProgress>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCQuestKillTargetProgress>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
