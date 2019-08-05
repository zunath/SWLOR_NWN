using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class SpawnObjectTypeCacheTests
    {
        private SpawnObjectTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SpawnObjectTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSpawnObjectType()
        {
            // Arrange
            SpawnObjectType entity = new SpawnObjectType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            SpawnObjectType entity1 = new SpawnObjectType { ID = 1};
            SpawnObjectType entity2 = new SpawnObjectType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            SpawnObjectType entity1 = new SpawnObjectType { ID = 1};
            SpawnObjectType entity2 = new SpawnObjectType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpawnObjectType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            SpawnObjectType entity1 = new SpawnObjectType { ID = 1};
            SpawnObjectType entity2 = new SpawnObjectType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpawnObjectType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpawnObjectType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpawnObjectType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
