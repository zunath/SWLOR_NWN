using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class DownloadCacheTests
    {
        private DownloadCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new DownloadCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsDownload()
        {
            // Arrange
            Download entity = new Download {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Download entity1 = new Download { ID = 1};
            Download entity2 = new Download { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Download entity1 = new Download { ID = 1};
            Download entity2 = new Download { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Download>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Download entity1 = new Download { ID = 1};
            Download entity2 = new Download { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Download>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Download>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Download>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
