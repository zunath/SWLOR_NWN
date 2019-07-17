using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class SpawnCacheTests
    {
        private SpawnCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SpawnCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSpawn()
        {
            // Arrange
            Spawn entity = new Spawn {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Spawn entity1 = new Spawn { ID = 1};
            Spawn entity2 = new Spawn { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Spawn entity1 = new Spawn { ID = 1};
            Spawn entity2 = new Spawn { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Spawn>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Spawn entity1 = new Spawn { ID = 1};
            Spawn entity2 = new Spawn { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Spawn>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Spawn>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Spawn>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
