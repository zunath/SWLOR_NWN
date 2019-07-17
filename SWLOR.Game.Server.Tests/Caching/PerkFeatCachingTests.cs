using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class PerkFeatCacheTests
    {
        private PerkFeatCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new PerkFeatCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsPerkFeat()
        {
            // Arrange
            PerkFeat entity = new PerkFeat {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            PerkFeat entity1 = new PerkFeat { ID = 1};
            PerkFeat entity2 = new PerkFeat { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            PerkFeat entity1 = new PerkFeat { ID = 1};
            PerkFeat entity2 = new PerkFeat { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkFeat>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            PerkFeat entity1 = new PerkFeat { ID = 1};
            PerkFeat entity2 = new PerkFeat { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<PerkFeat>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkFeat>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<PerkFeat>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
