using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PlantCacheTests
    {
        private PlantCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PlantCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPlant()
        {
            // Arrange
            Plant entity = new Plant {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity));

            // Assert
            Assert.AreSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Plant entity1 = new Plant { ID = 1};
            Plant entity2 = new Plant { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity2));

            // Assert
            Assert.AreSame(entity1, _cache.GetByID(1));
            Assert.AreSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Plant entity1 = new Plant { ID = 1};
            Plant entity2 = new Plant { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Plant>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Plant entity1 = new Plant { ID = 1};
            Plant entity2 = new Plant { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Plant>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Plant>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Plant>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
