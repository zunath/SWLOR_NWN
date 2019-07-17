using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class BuildingStyleCacheTests
    {
        private BuildingStyleCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new BuildingStyleCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsBuildingStyle()
        {
            // Arrange
            BuildingStyle entity = new BuildingStyle {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            BuildingStyle entity1 = new BuildingStyle { ID = 1};
            BuildingStyle entity2 = new BuildingStyle { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            BuildingStyle entity1 = new BuildingStyle { ID = 1};
            BuildingStyle entity2 = new BuildingStyle { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BuildingStyle>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            BuildingStyle entity1 = new BuildingStyle { ID = 1};
            BuildingStyle entity2 = new BuildingStyle { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BuildingStyle>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BuildingStyle>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BuildingStyle>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
