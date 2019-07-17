using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class SkillCacheTests
    {
        private SkillCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SkillCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSkill()
        {
            // Arrange
            Skill entity = new Skill {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Skill entity1 = new Skill { ID = 1};
            Skill entity2 = new Skill { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Skill entity1 = new Skill { ID = 1};
            Skill entity2 = new Skill { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Skill>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Skill entity1 = new Skill { ID = 1};
            Skill entity2 = new Skill { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Skill>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Skill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Skill>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
