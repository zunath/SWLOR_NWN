using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PerkLevelSkillRequirementCacheTests
    {
        private PerkLevelSkillRequirementCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PerkLevelSkillRequirementCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPerkLevelSkillRequirement()
        {
            // Arrange
            PerkLevelSkillRequirement entity = new PerkLevelSkillRequirement {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            PerkLevelSkillRequirement entity1 = new PerkLevelSkillRequirement { ID = 1};
            PerkLevelSkillRequirement entity2 = new PerkLevelSkillRequirement { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            PerkLevelSkillRequirement entity1 = new PerkLevelSkillRequirement { ID = 1};
            PerkLevelSkillRequirement entity2 = new PerkLevelSkillRequirement { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevelSkillRequirement>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            PerkLevelSkillRequirement entity1 = new PerkLevelSkillRequirement { ID = 1};
            PerkLevelSkillRequirement entity2 = new PerkLevelSkillRequirement { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkLevelSkillRequirement>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevelSkillRequirement>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkLevelSkillRequirement>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
