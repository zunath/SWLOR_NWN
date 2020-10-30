using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PerkLevelQuestRequirementCacheTests
    {
        private PerkLevelQuestRequirementCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PerkLevelQuestRequirementCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPerkLevelQuestRequirement()
        {
            // Arrange
            var entity = new PerkLevelQuestRequirement {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var entity1 = new PerkLevelQuestRequirement { ID = 1};
            var entity2 = new PerkLevelQuestRequirement { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var entity1 = new PerkLevelQuestRequirement { ID = 1};
            var entity2 = new PerkLevelQuestRequirement { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevelQuestRequirement>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var entity1 = new PerkLevelQuestRequirement { ID = 1};
            var entity2 = new PerkLevelQuestRequirement { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelQuestRequirement>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevelQuestRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevelQuestRequirement>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
