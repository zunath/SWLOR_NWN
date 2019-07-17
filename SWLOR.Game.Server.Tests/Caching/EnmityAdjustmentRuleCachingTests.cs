using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class EnmityAdjustmentRuleCacheTests
    {
        private EnmityAdjustmentRuleCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new EnmityAdjustmentRuleCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsEnmityAdjustmentRule()
        {
            // Arrange
            EnmityAdjustmentRule entity = new EnmityAdjustmentRule {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            EnmityAdjustmentRule entity1 = new EnmityAdjustmentRule { ID = 1};
            EnmityAdjustmentRule entity2 = new EnmityAdjustmentRule { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            EnmityAdjustmentRule entity1 = new EnmityAdjustmentRule { ID = 1};
            EnmityAdjustmentRule entity2 = new EnmityAdjustmentRule { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<EnmityAdjustmentRule>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            EnmityAdjustmentRule entity1 = new EnmityAdjustmentRule { ID = 1};
            EnmityAdjustmentRule entity2 = new EnmityAdjustmentRule { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<EnmityAdjustmentRule>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<EnmityAdjustmentRule>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<EnmityAdjustmentRule>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
