using System;
using System.Collections.Generic;
using System.Linq;
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
            Assert.AreNotSame(entity, _cache.GetByID(id));
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
            Assert.AreNotSame(entity1, _cache.GetByID(id1));
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
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
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
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

        [Test]
        public void GetAllByPCBaseID_ThreeItems_AreSame()
        {
            // Arrange
            var pcBaseID = Guid.NewGuid();
            var entity1 = new PCBaseStructure { ID = Guid.NewGuid(), PCBaseID = pcBaseID };
            var entity2 = new PCBaseStructure { ID = Guid.NewGuid(), PCBaseID = pcBaseID };
            var entity3 = new PCBaseStructure { ID = Guid.NewGuid(), PCBaseID = pcBaseID };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseStructure>(entity3));
            var results = _cache.GetAllByPCBaseID(pcBaseID).ToList();

            // Assert
            Assert.AreNotSame(entity1, results[0]);
            Assert.AreNotSame(entity2, results[1]);
            Assert.AreNotSame(entity3, results[2]);
        }

        [Test]
        public void GetAllByPCBaseID_NoItems_ReturnsEmptyList()
        {
            // Arrange
            var pcBaseID = Guid.NewGuid();

            // Act
            var results = _cache.GetAllByPCBaseID(pcBaseID);

            // Assert
            Assert.IsEmpty(results);
        }
    }
}
