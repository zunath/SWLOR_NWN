using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class KeyItemCacheTests
    {
        private KeyItemCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new KeyItemCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsKeyItem()
        {
            // Arrange
            KeyItem entity = new KeyItem {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            KeyItem entity1 = new KeyItem { ID = 1};
            KeyItem entity2 = new KeyItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            KeyItem entity1 = new KeyItem { ID = 1};
            KeyItem entity2 = new KeyItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<KeyItem>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            KeyItem entity1 = new KeyItem { ID = 1};
            KeyItem entity2 = new KeyItem { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<KeyItem>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<KeyItem>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<KeyItem>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
