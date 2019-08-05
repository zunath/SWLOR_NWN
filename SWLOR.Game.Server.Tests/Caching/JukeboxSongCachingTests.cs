using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class JukeboxSongCacheTests
    {
        private JukeboxSongCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new JukeboxSongCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void GetByID_OneItem_ReturnsJukeboxSong()
        {
            // Arrange
            JukeboxSong entity = new JukeboxSong {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.GetByID(1));
        }

        [Test]
        public void GetByID_TwoItems_ReturnsCorrectObject()
        {
            // Arrange
            JukeboxSong entity1 = new JukeboxSong { ID = 1};
            JukeboxSong entity2 = new JukeboxSong { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity2));

            // Assert
            Assert.AreNotSame(entity1, _cache.GetByID(1));
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_RemovedItem_ReturnsCorrectObject()
        {
            // Arrange
            JukeboxSong entity1 = new JukeboxSong { ID = 1};
            JukeboxSong entity2 = new JukeboxSong { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<JukeboxSong>(entity1));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.AreNotSame(entity2, _cache.GetByID(2));
        }

        [Test]
        public void GetByID_NoItems_ThrowsKeyNotFoundException()
        {
            // Arrange
            JukeboxSong entity1 = new JukeboxSong { ID = 1};
            JukeboxSong entity2 = new JukeboxSong { ID = 2};

            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectSet<JukeboxSong>(entity2));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<JukeboxSong>(entity1));
            MessageHub.Instance.Publish(new OnCacheObjectDeleted<JukeboxSong>(entity2));

            // Assert
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(1); });
            Assert.Throws<KeyNotFoundException>(() => { _cache.GetByID(2); });

        }
    }
}
