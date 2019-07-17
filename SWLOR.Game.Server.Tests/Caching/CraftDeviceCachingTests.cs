using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class CraftDeviceCacheTests
    {
        private CraftDeviceCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CraftDeviceCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsCraftDevice()
        {
            // Arrange
            CraftDevice entity = new CraftDevice {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            CraftDevice entity1 = new CraftDevice { ID = 1};
            CraftDevice entity2 = new CraftDevice { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            CraftDevice entity1 = new CraftDevice { ID = 1};
            CraftDevice entity2 = new CraftDevice { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftDevice>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            CraftDevice entity1 = new CraftDevice { ID = 1};
            CraftDevice entity2 = new CraftDevice { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftDevice>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftDevice>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftDevice>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
