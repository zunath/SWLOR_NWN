using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class DMActionTypeCacheTests
    {
        private DMActionTypeCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new DMActionTypeCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsDMActionType()
        {
            // Arrange
            DMActionType entity = new DMActionType {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            DMActionType entity1 = new DMActionType { ID = 1};
            DMActionType entity2 = new DMActionType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            DMActionType entity1 = new DMActionType { ID = 1};
            DMActionType entity2 = new DMActionType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<DMActionType>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            DMActionType entity1 = new DMActionType { ID = 1};
            DMActionType entity2 = new DMActionType { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMActionType>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<DMActionType>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<DMActionType>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
