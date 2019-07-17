using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PCBasePermissionCacheTests
    {
        private PCBasePermissionCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PCBasePermissionCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPCBasePermission()
        {
            // Arrange
            var id = Guid.NewGuid();
            PCBasePermission entity = new PCBasePermission {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBasePermission entity1 = new PCBasePermission { ID = id1};
            PCBasePermission entity2 = new PCBasePermission { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(id1));
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBasePermission entity1 = new PCBasePermission { ID = id1};
            PCBasePermission entity2 = new PCBasePermission { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBasePermission>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            PCBasePermission entity1 = new PCBasePermission { ID = id1};
            PCBasePermission entity2 = new PCBasePermission { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBasePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PCBasePermission>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });
        }

        [Test]
        public void GetPlayerPublicPermissionOrDefault_PublicAndPrivate_ShouldReturnPublic()
        {
            // Arrange
            var playerID = Guid.NewGuid();
            var baseID = Guid.NewGuid();
            var entity1 = new PCBasePermission { ID = Guid.NewGuid(), PlayerID = playerID, PCBaseID = baseID, IsPublicPermission = true };
            var entity2 = new PCBasePermission { ID = Guid.NewGuid(), PlayerID = playerID, PCBaseID = baseID, IsPublicPermission = false };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity2));

            // Assert
            var result = _cache.GetPlayerPrivatePermissionOrDefault(playerID, baseID);
            Assert.AreNotSame(entity2, result);
        }

        [Test]
        public void GetPlayerPublicPermissionOrDefault_Private_ShouldReturnNull()
        {
            // Arrange
            var playerID = Guid.NewGuid();
            var baseID = Guid.NewGuid();
            var entity1 = new PCBasePermission { ID = Guid.NewGuid(), PlayerID = playerID, PCBaseID = baseID, IsPublicPermission = true };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity1));

            // Assert
            var result = _cache.GetPlayerPrivatePermissionOrDefault(playerID, baseID);
            Assert.AreSame(null, result);
        }

        [Test]
        public void GetPlayerPublicPermissionOrDefault_TwoPublic_ShouldReturnNull()
        {
            // Arrange
            var playerID = Guid.NewGuid();
            var baseID = Guid.NewGuid();
            var entity1 = new PCBasePermission { ID = Guid.NewGuid(), PlayerID = playerID, PCBaseID = baseID, IsPublicPermission = true };
            var entity2 = new PCBasePermission { ID = Guid.NewGuid(), PlayerID = playerID, PCBaseID = baseID, IsPublicPermission = true };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PCBasePermission>(entity2));

            // Assert
            var result = _cache.GetPlayerPrivatePermissionOrDefault(playerID, baseID);
            Assert.AreSame(null, result);
        }


        [Test]
        public void GetPlayerPublicPermissionOrDefault_None_ShouldReturnNull()
        {
            // Arrange
            var playerID = Guid.NewGuid();
            var baseID = Guid.NewGuid();
            
            // Act

            // Assert
            var result = _cache.GetPlayerPrivatePermissionOrDefault(playerID, baseID);
            Assert.AreSame(null, result);
        }

    }
}
