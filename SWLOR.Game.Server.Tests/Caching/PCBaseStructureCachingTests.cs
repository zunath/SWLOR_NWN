using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCBaseStructureCacheTests
    {
        private PCBaseStructureCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCBaseStructureCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCBaseStructure()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCBaseStructure entity = new PCBaseStructure {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity));

            // Assert
            Assert.AreSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBaseStructure entity1 = new PCBaseStructure { ID = id1};
            PCBaseStructure entity2 = new PCBaseStructure { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity2));

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
            PCBaseStructure entity1 = new PCBaseStructure { ID = id1};
            PCBaseStructure entity2 = new PCBaseStructure { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructure>(entity1));

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
            PCBaseStructure entity1 = new PCBaseStructure { ID = id1};
            PCBaseStructure entity2 = new PCBaseStructure { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseStructure>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
