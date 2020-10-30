using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class StarportCacheTests
    {
        private StarportCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new StarportCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSpaceStarport()
        {
            // Arrange
            var id = 555;
            var entity = new Starport { ID = id, PlanetName = "My Planet"};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = 100;
            var id2 = 500;
            var entity1 = new Starport { ID = id1, PlanetName = "My Planet" };
            var entity2 = new Starport { ID = id2, PlanetName = "My Planet 2" };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(id1));
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = 100;
            var id2 = 200;
            var entity1 = new Starport { ID = id1, PlanetName = "My Planet" };
            var entity2 = new Starport { ID = id2, PlanetName = "My Planet" };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Starport>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = 100;
            var id2 = 200;
            var entity1 = new Starport { ID = id1, PlanetName = "My Planet" };
            var entity2 = new Starport { ID = id2, PlanetName = "My Planet" };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Starport>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Starport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Starport>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
