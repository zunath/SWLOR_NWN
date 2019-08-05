using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class SpaceEncounterCacheTests
    {
        private SpaceEncounterCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new SpaceEncounterCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsSpaceEncounter()
        {
            // Arrange
            SpaceEncounter entity = new SpaceEncounter {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            SpaceEncounter entity1 = new SpaceEncounter { ID = 1};
            SpaceEncounter entity2 = new SpaceEncounter { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            SpaceEncounter entity1 = new SpaceEncounter { ID = 1};
            SpaceEncounter entity2 = new SpaceEncounter { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpaceEncounter>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            SpaceEncounter entity1 = new SpaceEncounter { ID = 1};
            SpaceEncounter entity2 = new SpaceEncounter { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<SpaceEncounter>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpaceEncounter>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<SpaceEncounter>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
