using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class UserCacheTests
    {
        private UserCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new UserCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsUser()
        {
            // Arrange
            User entity = new User {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity));

            // Assert
            Assert.AreSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            User entity1 = new User { ID = 1};
            User entity2 = new User { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity2));

            // Assert
            Assert.AreSame(entity1, _cache.GetByID(1));
            Assert.AreSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            User entity1 = new User { ID = 1};
            User entity2 = new User { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<User>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            User entity1 = new User { ID = 1};
            User entity2 = new User { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<User>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<User>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<User>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
