using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PlayerCacheTests
    {
        private PlayerCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PlayerCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPlayer()
        {
            // Arrange
            var id = Guid.NewGuid();
            Player entity = new Player {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            Player entity1 = new Player { ID = id1};
            Player entity2 = new Player { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity2));

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
            Player entity1 = new Player { ID = id1};
            Player entity2 = new Player { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Player>(entity1));

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
            Player entity1 = new Player { ID = id1};
            Player entity2 = new Player { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Player>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Player>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });
        }

        [Test]
        public void GetByPrimaryResidenceStructureIDOrDefault_NoEntries_ShouldReturnNull()
        {
            // Arrange
            Guid id = Guid.NewGuid();

            // Act
            var result = _cache.GetByPrimaryResidencePCBaseStructureIDOrDefault(id);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetByPrimaryResidenceStructureIDOrDefault_TwoEntries_ShouldReturnPlayer()
        {
            // Arrange
            Guid playerID = Guid.NewGuid();
            Guid structureID = Guid.NewGuid();
            var player = new Player { ID = playerID, PrimaryResidencePCBaseStructureID = structureID };
            var player2 = new Player { ID = Guid.NewGuid() };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(player));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Player>(player2));

            var result = _cache.GetByPrimaryResidencePCBaseStructureIDOrDefault(structureID);

            // Assert
            Assert.AreNotSame(player, result);
        }

    }
}
