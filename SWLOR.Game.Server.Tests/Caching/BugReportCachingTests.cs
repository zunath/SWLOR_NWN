using System;
using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class BugReportCacheTests
    {
        private BugReportCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new BugReportCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsBugReport()
        {
            // Arrange
            var id = Guid.NewGuid();
            BugReport entity = new BugReport {ID = id};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(id));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            BugReport entity1 = new BugReport { ID = id1};
            BugReport entity2 = new BugReport { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(id1));
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            BugReport entity1 = new BugReport { ID = id1};
            BugReport entity2 = new BugReport { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BugReport>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.AreNotSame(entity2, _cache.GetByID(id2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();
            BugReport entity1 = new BugReport { ID = id1};
            BugReport entity2 = new BugReport { ID = id2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<BugReport>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BugReport>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<BugReport>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(id2); });

        }
    }
}
