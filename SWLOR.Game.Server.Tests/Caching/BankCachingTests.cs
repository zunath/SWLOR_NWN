using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class BankCacheTests
    {
        private BankCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new BankCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsBank()
        {
            // Arrange
            Bank entity = new Bank {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            Bank entity1 = new Bank { ID = 1};
            Bank entity2 = new Bank { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            Bank entity1 = new Bank { ID = 1};
            Bank entity2 = new Bank { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Bank>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            Bank entity1 = new Bank { ID = 1};
            Bank entity2 = new Bank { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<Bank>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Bank>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<Bank>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
