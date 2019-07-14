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

        [Test]
        public void GetByResref_OneItem_ReturnsArea()
        {
            // Arrange
            var resref = "testarea";
            var entity1 = new Area {ID = Guid.NewGuid(), Resref = resref};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity1));
            var result = _cache.GetByResref(resref);

            // Assert
            Assert.AreSame(result, entity1);
        }

        [Test]
        public void GetByResref_TwoItems_ReturnsSecondArea()
        {
            // Arrange
            var resref1 = "testarea1";
            var resref2 = "testarea2";
            var entity1 = new Area { ID = Guid.NewGuid(), Resref = resref1 };
            var entity2 = new Area { ID = Guid.NewGuid(), Resref = resref2 };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity2));
            var result2 = _cache.GetByResref(resref2);

            // Assert
            Assert.AreSame(result2, entity2);
        }

        [Test]
        public void GetByResref_NullKey_ThrowsKeyNotFoundException()
        {
            // Arrange
            var resref1 = "testarea1";
            var resref2 = "testarea2";
            var entity1 = new Area { ID = Guid.NewGuid(), Resref = resref1 };
            var entity2 = new Area { ID = Guid.NewGuid(), Resref = resref2 };

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Area>(entity2));

            // Assert
            Assert.Throws<ArgumentNullException>(() =>
            {
                _cache.GetByResref(null);
            }) ;


        }
    }
}
