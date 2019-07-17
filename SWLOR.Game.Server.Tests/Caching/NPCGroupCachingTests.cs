using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class NPCGroupCacheTests
    {
        private NPCGroupCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new NPCGroupCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsNPCGroup()
        {
            // Arrange
            NPCGroup entity = new NPCGroup {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            NPCGroup entity1 = new NPCGroup { ID = 1};
            NPCGroup entity2 = new NPCGroup { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            NPCGroup entity1 = new NPCGroup { ID = 1};
            NPCGroup entity2 = new NPCGroup { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<NPCGroup>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            NPCGroup entity1 = new NPCGroup { ID = 1};
            NPCGroup entity2 = new NPCGroup { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<NPCGroup>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<NPCGroup>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<NPCGroup>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
