using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCOutfitCacheTests
    {
        private PCOutfitCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCOutfitCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCOutfit()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCOutfit entity = new PCOutfit {PlayerID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCOutfit entity1 = new PCOutfit { PlayerID = id1};
            PCOutfit entity2 = new PCOutfit { PlayerID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity2));

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
            PCOutfit entity1 = new PCOutfit { PlayerID = id1};
            PCOutfit entity2 = new PCOutfit { PlayerID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCOutfit>(entity1));

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
            PCOutfit entity1 = new PCOutfit { PlayerID = id1};
            PCOutfit entity2 = new PCOutfit { PlayerID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCOutfit>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCOutfit>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCOutfit>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
