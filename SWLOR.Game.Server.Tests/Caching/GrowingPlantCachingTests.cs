using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class GrowingPlantCacheTests
    {
        private GrowingPlantCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new GrowingPlantCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsGrowingPlant()
        {
            // Arrange
            var id = Guid.NewGuid();
            GrowingPlant entity = new GrowingPlant {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity));

            // Assert
            Assert.AreSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            GrowingPlant entity1 = new GrowingPlant { ID = id1};
            GrowingPlant entity2 = new GrowingPlant { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity2));

            // Assert
            Assert.AreSame(entity1, _cache.GetByID(id1));
            Assert.AreSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            GrowingPlant entity1 = new GrowingPlant { ID = id1};
            GrowingPlant entity2 = new GrowingPlant { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GrowingPlant>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            GrowingPlant entity1 = new GrowingPlant { ID = id1};
            GrowingPlant entity2 = new GrowingPlant { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GrowingPlant>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GrowingPlant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GrowingPlant>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
