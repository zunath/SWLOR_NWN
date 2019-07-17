using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCMapProgressionCacheTests
    {
        private PCMapProgressionCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCMapProgressionCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCMapProgression()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCMapProgression entity = new PCMapProgression {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCMapProgression entity1 = new PCMapProgression { ID = id1};
            PCMapProgression entity2 = new PCMapProgression { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity2));

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
            PCMapProgression entity1 = new PCMapProgression { ID = id1};
            PCMapProgression entity2 = new PCMapProgression { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCMapProgression>(entity1));

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
            PCMapProgression entity1 = new PCMapProgression { ID = id1};
            PCMapProgression entity2 = new PCMapProgression { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCMapProgression>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCMapProgression>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCMapProgression>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
