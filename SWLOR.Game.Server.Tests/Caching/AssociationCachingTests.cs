using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class AssociationCacheTests
    {
        private AssociationCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new AssociationCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsAssociation()
        {
            // Arrange
            Association entity = new Association {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Association entity1 = new Association { ID = 1};
            Association entity2 = new Association { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Association entity1 = new Association { ID = 1};
            Association entity2 = new Association { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Association>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Association entity1 = new Association { ID = 1};
            Association entity2 = new Association { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Association>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Association>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Association>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
