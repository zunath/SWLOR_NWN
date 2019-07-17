using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class StructureModeCacheTests
    {
        private StructureModeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new StructureModeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsStructureMode()
        {
            // Arrange
            StructureMode entity = new StructureMode {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            StructureMode entity1 = new StructureMode { ID = 1};
            StructureMode entity2 = new StructureMode { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            StructureMode entity1 = new StructureMode { ID = 1};
            StructureMode entity2 = new StructureMode { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<StructureMode>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            StructureMode entity1 = new StructureMode { ID = 1};
            StructureMode entity2 = new StructureMode { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<StructureMode>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<StructureMode>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<StructureMode>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
