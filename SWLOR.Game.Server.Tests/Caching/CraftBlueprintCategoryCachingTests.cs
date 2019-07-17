using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class CraftBlueprintCategoryCacheTests
    {
        private CraftBlueprintCategoryCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CraftBlueprintCategoryCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsCraftBlueprintCategory()
        {
            // Arrange
            CraftBlueprintCategory entity = new CraftBlueprintCategory {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            CraftBlueprintCategory entity1 = new CraftBlueprintCategory { ID = 1};
            CraftBlueprintCategory entity2 = new CraftBlueprintCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            CraftBlueprintCategory entity1 = new CraftBlueprintCategory { ID = 1};
            CraftBlueprintCategory entity2 = new CraftBlueprintCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftBlueprintCategory>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            CraftBlueprintCategory entity1 = new CraftBlueprintCategory { ID = 1};
            CraftBlueprintCategory entity2 = new CraftBlueprintCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprintCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftBlueprintCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftBlueprintCategory>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
