using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Domain.Properties.Contracts;
using SWLOR.Test.Shared;
using SWLOR.NWN.API.Service;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class AreaCacheServiceTests : TestBase
    {
        private IPropertyService _mockPropertyService;
        private IGenericCacheService _mockCacheService;
        private IServiceProvider _mockServiceProvider;
        private AreaCacheService _service;

        [SetUp]
        public void SetUp()
        {
            _mockPropertyService = Substitute.For<IPropertyService>();
            _mockCacheService = Substitute.For<IGenericCacheService>();
            
            var services = new ServiceCollection();
            services.AddSingleton(_mockPropertyService);
            services.AddSingleton(_mockCacheService);
            _mockServiceProvider = services.BuildServiceProvider();
            
            _service = new AreaCacheService(_mockServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            (_mockServiceProvider as IDisposable)?.Dispose();
        }

        [Test]
        public void Constructor_ShouldInitializeService()
        {
            // Act & Assert
            Assert.That(_service, Is.Not.Null);
        }

        [Test]
        public void LoadCache_ShouldLoadAreas()
        {
            // Arrange
            // Set up mock data for areas
            NWScript.GetFirstArea().Returns(1u);
            NWScript.GetNextArea().Returns(2u, 0u);
            NWScript.GetIsObjectValid(1u).Returns(true);
            NWScript.GetIsObjectValid(2u).Returns(false);
            NWScript.GetResRef(1u).Returns("test_area");

            // Act
            _service.LoadCache();

            // Assert
            var areas = _service.GetAreas();
            Assert.That(areas, Is.Not.Null);
            Assert.That(areas.Count, Is.GreaterThan(0));
        }

        [Test]
        public void GetAreaByResref_ShouldReturnArea()
        {
            // Arrange
            NWScript.GetFirstArea().Returns(1u);
            NWScript.GetNextArea().Returns(0u);
            NWScript.GetIsObjectValid(1u).Returns(true);
            NWScript.GetResRef(1u).Returns("test_area");
            _service.LoadCache();

            // Act
            var result = _service.GetAreaByResref("test_area");

            // Assert
            Assert.That(result, Is.EqualTo(1u));
        }

        [Test]
        public void GetAreaByResref_WithInvalidResref_ShouldReturnZero()
        {
            // Arrange
            NWScript.GetFirstArea().Returns(1u);
            NWScript.GetNextArea().Returns(0u);
            NWScript.GetIsObjectValid(1u).Returns(true);
            NWScript.GetResRef(1u).Returns("test_area");
            _service.LoadCache();

            // Act
            var result = _service.GetAreaByResref("invalid_area");

            // Assert
            Assert.That(result, Is.EqualTo(0u));
        }

        [Test]
        public void EnterArea_ShouldAddPlayer()
        {
            // Arrange
            var playerId = 123u;
            var areaId = 456u;

            // Act
            _service.EnterArea(playerId, areaId);

            // Assert
            var players = _service.GetPlayersInArea(areaId);
            Assert.That(players, Contains.Item(playerId));
        }

        [Test]
        public void ExitArea_ShouldRemovePlayer()
        {
            // Arrange
            var playerId = 123u;
            var areaId = 456u;
            _service.EnterArea(playerId, areaId);

            // Act
            _service.ExitArea(playerId, areaId);

            // Assert
            var players = _service.GetPlayersInArea(areaId);
            Assert.That(players, Does.Not.Contain(playerId));
        }

        [Test]
        public void GetPlayersInArea_ShouldReturnPlayers()
        {
            // Arrange
            var playerId1 = 123u;
            var playerId2 = 456u;
            var areaId = 789u;
            _service.EnterArea(playerId1, areaId);
            _service.EnterArea(playerId2, areaId);

            // Act
            var players = _service.GetPlayersInArea(areaId);

            // Assert
            Assert.That(players, Contains.Item(playerId1));
            Assert.That(players, Contains.Item(playerId2));
        }

        [Test]
        public void GetPlayersInArea_WithNoPlayers_ShouldReturnEmptyList()
        {
            // Arrange
            var areaId = 789u;

            // Act
            var players = _service.GetPlayersInArea(areaId);

            // Assert
            Assert.That(players, Is.Empty);
        }

        [Test]
        public void GetAreas_ShouldReturnCachedAreas()
        {
            // Arrange
            NWScript.GetFirstArea().Returns(1u);
            NWScript.GetNextArea().Returns(0u);
            NWScript.GetIsObjectValid(1u).Returns(true);
            NWScript.GetResRef(1u).Returns("test_area");
            _service.LoadCache();

            // Act
            var result = _service.GetAreas();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ContainsKey("test_area"), Is.True);
        }
    }
}
