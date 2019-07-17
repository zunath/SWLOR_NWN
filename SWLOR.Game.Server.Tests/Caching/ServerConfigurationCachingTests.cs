using System.Collections.Generic;
using NUnit.Framework;
using SWLOR.Game.Server.Caching;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Tests.Caching
{
    public class ServerConfigurationCacheTests
    {
        private ServerConfigurationCache _cache;

        [SetUp]
        public void Setup()
        {
            _cache = new ServerConfigurationCache();
        }

        [TearDown]
        public void TearDown()
        {
            _cache = null;
        }


        [Test]
        public void Get_ReturnsServerConfiguration()
        {
            // Arrange
            ServerConfiguration entity = new ServerConfiguration {ID = 1};
            
            // Act
            MessageHub.Instance.Publish(new OnCacheObjectSet<ServerConfiguration>(entity));

            // Assert
            Assert.AreNotSame(entity, _cache.Get());
        }

    }
}
