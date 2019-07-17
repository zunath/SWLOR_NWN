using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class GameTopicCategoryCacheTests
    {
        private GameTopicCategoryCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new GameTopicCategoryCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsGameTopicCategory()
        {
            // Arrange
            GameTopicCategory entity = new GameTopicCategory {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            GameTopicCategory entity1 = new GameTopicCategory { ID = 1};
            GameTopicCategory entity2 = new GameTopicCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            GameTopicCategory entity1 = new GameTopicCategory { ID = 1};
            GameTopicCategory entity2 = new GameTopicCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GameTopicCategory>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            GameTopicCategory entity1 = new GameTopicCategory { ID = 1};
            GameTopicCategory entity2 = new GameTopicCategory { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<GameTopicCategory>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GameTopicCategory>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<GameTopicCategory>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
