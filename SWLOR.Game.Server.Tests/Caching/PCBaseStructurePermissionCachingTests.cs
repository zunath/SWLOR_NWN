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
    public class PCBaseStructurePermissionCacheTests
    {
        private PCBaseStructurePermissionCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCBaseStructurePermissionCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCBaseStructurePermission()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCBaseStructurePermission entity = new PCBaseStructurePermission {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBaseStructurePermission entity1 = new PCBaseStructurePermission { ID = id1};
            PCBaseStructurePermission entity2 = new PCBaseStructurePermission { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity2));

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
            PCBaseStructurePermission entity1 = new PCBaseStructurePermission { ID = id1};
            PCBaseStructurePermission entity2 = new PCBaseStructurePermission { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructurePermission>(entity1));

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
            PCBaseStructurePermission entity1 = new PCBaseStructurePermission { ID = id1};
            PCBaseStructurePermission entity2 = new PCBaseStructurePermission { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructurePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructurePermission>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });
        }

        [Test]
        public void GetAllByPlayerID_ThreeItems_ShouldReturnSameItems()
        {
            // Arrange
            var playerID = Guid.NewGuid();
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            var id3 = Guid.NewGuid();
            var id4 = Guid.NewGuid();
            PCBaseStructurePermission entity1 = new PCBaseStructurePermission { ID = id1, PlayerID = playerID };
            PCBaseStructurePermission entity2 = new PCBaseStructurePermission { ID = id2, PlayerID = playerID };
            PCBaseStructurePermission entity3 = new PCBaseStructurePermission { ID = id3, PlayerID = playerID };
            PCBaseStructurePermission entity4 = new PCBaseStructurePermission { ID = id4, PlayerID = playerID };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity3));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructurePermission>(entity4));

            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructurePermission>(entity2));

            // Assert
            var results = _cache.GetAllByPlayerID(playerID).ToList();
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(entity1.ID, results.ElementAt(0).ID);
            Assert.AreEqual(entity3.ID, results.ElementAt(1).ID);
            Assert.AreEqual(entity4.ID, results.ElementAt(2).ID);
        }
    }
}
