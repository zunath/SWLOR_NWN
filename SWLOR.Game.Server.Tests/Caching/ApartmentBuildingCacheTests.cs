using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class ApartmentBuildingCacheTests
    {
        private ApartmentBuildingCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new ApartmentBuildingCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsApartmentBuilding()
        {
            // Arrange
            ApartmentBuilding building = new ApartmentBuilding {ID = 1, Name = "MyBuilding"};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building));

            // Assert
            Assert.AreNotSame(building, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            ApartmentBuilding building1 = new ApartmentBuilding { ID = 1, Name = "Building1" };
            ApartmentBuilding building2 = new ApartmentBuilding { ID = 2, Name = "Building2" };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building2));

            // Assert
            Assert.AreNotSame(building1, _cache.GetByID(1));
            Assert.AreNotSame(building2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            ApartmentBuilding building1 = new ApartmentBuilding { ID = 1, Name = "Building1" };
            ApartmentBuilding building2 = new ApartmentBuilding { ID = 2, Name = "Building2" };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ApartmentBuilding>(building1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(building2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            ApartmentBuilding building1 = new ApartmentBuilding { ID = 1, Name = "Building1" };
            ApartmentBuilding building2 = new ApartmentBuilding { ID = 2, Name = "Building2" };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<ApartmentBuilding>(building2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ApartmentBuilding>(building1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<ApartmentBuilding>(building2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
