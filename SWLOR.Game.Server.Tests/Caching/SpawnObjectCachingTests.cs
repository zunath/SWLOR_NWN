using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class SpawnObjectCacheTests
    {
        private SpawnObjectCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SpawnObjectCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSpawnObject()
        {
            // Arrange
            SpawnObject entity = new SpawnObject {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            SpawnObject entity1 = new SpawnObject { ID = 1};
            SpawnObject entity2 = new SpawnObject { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            SpawnObject entity1 = new SpawnObject { ID = 1};
            SpawnObject entity2 = new SpawnObject { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpawnObject>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            SpawnObject entity1 = new SpawnObject { ID = 1};
            SpawnObject entity2 = new SpawnObject { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObject>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpawnObject>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpawnObject>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
