using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCBaseTypeCacheTests
    {
        private PCBaseTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCBaseTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCBaseType()
        {
            // Arrange
            PCBaseType entity = new PCBaseType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            PCBaseType entity1 = new PCBaseType { ID = 1};
            PCBaseType entity2 = new PCBaseType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            PCBaseType entity1 = new PCBaseType { ID = 1};
            PCBaseType entity2 = new PCBaseType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            PCBaseType entity1 = new PCBaseType { ID = 1};
            PCBaseType entity2 = new PCBaseType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBaseType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBaseType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
