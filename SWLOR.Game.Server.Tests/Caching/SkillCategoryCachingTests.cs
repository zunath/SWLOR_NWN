using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class SkillCategoryCacheTests
    {
        private SkillCategoryCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SkillCategoryCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSkillCategory()
        {
            // Arrange
            SkillCategory entity = new SkillCategory {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            SkillCategory entity1 = new SkillCategory { ID = 1};
            SkillCategory entity2 = new SkillCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            SkillCategory entity1 = new SkillCategory { ID = 1};
            SkillCategory entity2 = new SkillCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SkillCategory>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            SkillCategory entity1 = new SkillCategory { ID = 1};
            SkillCategory entity2 = new SkillCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SkillCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SkillCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SkillCategory>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
