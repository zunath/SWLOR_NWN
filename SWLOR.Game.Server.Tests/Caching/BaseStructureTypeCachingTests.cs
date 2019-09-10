using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class BaseStructureTypeCacheTests
    {
        private BaseStructureTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new BaseStructureTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsBaseStructureType()
        {
            // Arrange
            BaseStructureType entity = new BaseStructureType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            BaseStructureType entity1 = new BaseStructureType { ID = 1};
            BaseStructureType entity2 = new BaseStructureType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            BaseStructureType entity1 = new BaseStructureType { ID = 1};
            BaseStructureType entity2 = new BaseStructureType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BaseStructureType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            BaseStructureType entity1 = new BaseStructureType { ID = 1};
            BaseStructureType entity2 = new BaseStructureType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BaseStructureType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BaseStructureType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BaseStructureType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
