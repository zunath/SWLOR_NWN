using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class BuildingTypeCacheTests
    {
        private BuildingTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new BuildingTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsBuildingType()
        {
            // Arrange
            BuildingType entity = new BuildingType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            BuildingType entity1 = new BuildingType { ID = 1};
            BuildingType entity2 = new BuildingType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            BuildingType entity1 = new BuildingType { ID = 1};
            BuildingType entity2 = new BuildingType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BuildingType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            BuildingType entity1 = new BuildingType { ID = 1};
            BuildingType entity2 = new BuildingType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BuildingType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BuildingType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
