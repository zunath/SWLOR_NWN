using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class AuthorizedDMCacheTests
    {
        private AuthorizedDMCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new AuthorizedDMCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsAuthorizedDM()
        {
            // Arrange
            AuthorizedDM entity = new AuthorizedDM {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            AuthorizedDM entity1 = new AuthorizedDM { ID = 1};
            AuthorizedDM entity2 = new AuthorizedDM { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            AuthorizedDM entity1 = new AuthorizedDM { ID = 1};
            AuthorizedDM entity2 = new AuthorizedDM { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<AuthorizedDM>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            AuthorizedDM entity1 = new AuthorizedDM { ID = 1};
            AuthorizedDM entity2 = new AuthorizedDM { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<AuthorizedDM>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<AuthorizedDM>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<AuthorizedDM>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
