using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCSkillCacheTests
    {
        private PCSkillCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCSkillCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCSkill()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCSkill entity = new PCSkill {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCSkill entity1 = new PCSkill { ID = id1};
            PCSkill entity2 = new PCSkill { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity2));

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
            PCSkill entity1 = new PCSkill { ID = id1};
            PCSkill entity2 = new PCSkill { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCSkill>(entity1));

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
            PCSkill entity1 = new PCSkill { ID = id1};
            PCSkill entity2 = new PCSkill { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCSkill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCSkill>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }

        [Test]
        public void GetAllByPlayerID_NoItems_ReturnsEmptyEnumerable()
        {
            // Arrange
            var id = Guid.NewGuid();

            // Act
            var results = _cache.GetAllByPlayerID(id);

            // Assert
            Assert.IsEmpty(results);
        }

        [Test]
        public void GetAllByPlayerID_ThreeItemsSamePlayer_ReturnsThreeItems()
        {
            // Arrange
            var id = Guid.NewGuid();
            var entity1 = new PCSkill { ID = Guid.NewGuid(), PlayerID = id };
            var entity2 = new PCSkill { ID = Guid.NewGuid(), PlayerID = id };
            var entity3 = new PCSkill { ID = Guid.NewGuid(), PlayerID = id };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity3));
            var result = _cache.GetAllByPlayerID(id).ToList();

            // Assert
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(entity1.ID, result.ElementAt(0).ID);
            Assert.AreEqual(entity2.ID, result.ElementAt(1).ID);
            Assert.AreEqual(entity3.ID, result.ElementAt(2).ID);
        }

        [Test]
        public void GetAllByPlayerID_TwoPlayers_ReturnsTwoItems()
        {
            // Arrange
            var id = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var entity1 = new PCSkill { ID = Guid.NewGuid(), PlayerID = id };
            var entity2 = new PCSkill { ID = Guid.NewGuid(), PlayerID = id2 };
            var entity3 = new PCSkill { ID = Guid.NewGuid(), PlayerID = id };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCSkill>(entity3));
            var result = _cache.GetAllByPlayerID(id).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(entity1.ID, result.ElementAt(0).ID);
            Assert.AreEqual(entity3.ID, result.ElementAt(1).ID);
        }

    }
}
