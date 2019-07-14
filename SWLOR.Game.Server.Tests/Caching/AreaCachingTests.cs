using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class AreaCacheTests
    {
        private AreaCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new AreaCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsArea()
        {
            // Arrange
            var id = Guid.NewGuid();
            Area entity = new Area {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity));

            // Assert
            Assert.AreSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            Area entity1 = new Area { ID = id1};
            Area entity2 = new Area { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity2));

            // Assert
            Assert.AreSame(entity1, _cache.GetByID(id1));
            Assert.AreSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            Area entity1 = new Area { ID = id1};
            Area entity2 = new Area { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Area>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            Area entity1 = new Area { ID = id1};
            Area entity2 = new Area { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Area>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Area>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
