using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class DMRoleCacheTests
    {
        private DMRoleCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new DMRoleCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsDMRole()
        {
            // Arrange
            DMRole entity = new DMRole {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            DMRole entity1 = new DMRole { ID = 1};
            DMRole entity2 = new DMRole { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            DMRole entity1 = new DMRole { ID = 1};
            DMRole entity2 = new DMRole { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<DMRole>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            DMRole entity1 = new DMRole { ID = 1};
            DMRole entity2 = new DMRole { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<DMRole>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<DMRole>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<DMRole>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
