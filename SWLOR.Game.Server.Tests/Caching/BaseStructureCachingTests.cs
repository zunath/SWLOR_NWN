using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class BaseStructureCacheTests
    {
        private BaseStructureCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new BaseStructureCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsBaseStructure()
        {
            // Arrange
            BaseStructure entity = new BaseStructure {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            BaseStructure entity1 = new BaseStructure { ID = 1};
            BaseStructure entity2 = new BaseStructure { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            BaseStructure entity1 = new BaseStructure { ID = 1};
            BaseStructure entity2 = new BaseStructure { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BaseStructure>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            BaseStructure entity1 = new BaseStructure { ID = 1};
            BaseStructure entity2 = new BaseStructure { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructure>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BaseStructure>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BaseStructure>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
