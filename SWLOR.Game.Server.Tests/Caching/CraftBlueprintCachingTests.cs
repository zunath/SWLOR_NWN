using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class CraftBlueprintCacheTests
    {
        private CraftBlueprintCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new CraftBlueprintCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsCraftBlueprint()
        {
            // Arrange
            CraftBlueprint entity = new CraftBlueprint {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            CraftBlueprint entity1 = new CraftBlueprint { ID = 1};
            CraftBlueprint entity2 = new CraftBlueprint { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            CraftBlueprint entity1 = new CraftBlueprint { ID = 1};
            CraftBlueprint entity2 = new CraftBlueprint { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftBlueprint>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            CraftBlueprint entity1 = new CraftBlueprint { ID = 1};
            CraftBlueprint entity2 = new CraftBlueprint { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<CraftBlueprint>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftBlueprint>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<CraftBlueprint>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
