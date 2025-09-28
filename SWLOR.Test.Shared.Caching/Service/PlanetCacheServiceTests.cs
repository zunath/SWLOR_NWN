using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Test.Shared;
using System; // Added for IDisposable

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class PlanetCacheServiceTests : TestBase
    {
        private IGenericCacheService _mockCacheService;
        private IEnumCache<PlanetType, PlanetAttribute> _mockEnumCache;
        private IServiceProvider _mockServiceProvider;
        private PlanetCacheService _service;

        [SetUp]
        public void SetUp()
        {
            _mockCacheService = Substitute.For<IGenericCacheService>();
            _mockEnumCache = Substitute.For<IEnumCache<PlanetType, PlanetAttribute>>();
            
            // Create a real service collection and register our mock
            var services = new ServiceCollection();
            services.AddSingleton(_mockCacheService);
            _mockServiceProvider = services.BuildServiceProvider();
            
            // Create the service after setting up the mocks
            _service = new PlanetCacheService(_mockServiceProvider);
        }

        [TearDown]
        public void TearDown()
        {
            (_mockServiceProvider as IDisposable)?.Dispose();
        }

        [Test]
        public void InitializeCache_ShouldBuildEnumCacheAndPopulateAllPlanets()
        {
            // Arrange
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) },
                { PlanetType.Tatooine, new PlanetAttribute("Tatooine", "Tatooine - ", "Tatooine_Orbit", "TATOOINE_LANDING", 400, true) },
                { PlanetType.Invalid, new PlanetAttribute("Invalid", "-- Invalid --", "", "", 0, false) }
            };

            var mockCacheBuilder = Substitute.For<IEnumCacheBuilder<PlanetType, PlanetAttribute>>();
            var mockFilteredCache = Substitute.For<IEnumCacheBuilder<PlanetType, PlanetAttribute>>();
            
            _mockCacheService.BuildEnumCache<PlanetType, PlanetAttribute>().Returns(mockCacheBuilder);
            mockCacheBuilder.WithAllItems().Returns(mockCacheBuilder);
            mockCacheBuilder.WithFilteredCache("Active", Arg.Any<Func<PlanetAttribute, bool>>()).Returns(mockFilteredCache);
            mockFilteredCache.Build().Returns(_mockEnumCache);
            _mockEnumCache.AllItems.Returns(mockPlanets);

            // Act
            _service.InitializeCache();

            // Assert
            _mockCacheService.Received(1).BuildEnumCache<PlanetType, PlanetAttribute>();
            mockCacheBuilder.Received(1).WithAllItems();
            mockCacheBuilder.Received(1).WithFilteredCache("Active", Arg.Any<Func<PlanetAttribute, bool>>());
            mockFilteredCache.Received(1).Build();
        }

        [Test]
        public void GetPlanetByType_WithValidType_ShouldReturnPlanetAttribute()
        {
            // Arrange
            var expectedPlanet = new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true);
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, expectedPlanet }
            };

            SetupMockCache(mockPlanets);

            // Act
            var result = _service.GetPlanetByType(PlanetType.Viscara);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlanet));
        }

        [Test]
        public void GetPlanetByType_WithInvalidType_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>();
            SetupMockCache(mockPlanets);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.GetPlanetByType(PlanetType.Viscara));
        }

        [Test]
        public void GetAllPlanets_ShouldReturnAllPlanets()
        {
            // Arrange
            var expectedPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) },
                { PlanetType.Tatooine, new PlanetAttribute("Tatooine", "Tatooine - ", "Tatooine_Orbit", "TATOOINE_LANDING", 400, true) },
                { PlanetType.Invalid, new PlanetAttribute("Invalid", "-- Invalid --", "", "", 0, false) }
            };

            SetupMockCache(expectedPlanets);

            // Act
            var result = _service.GetAllPlanets();

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlanets));
        }

        [Test]
        public void GetActivePlanets_ShouldReturnOnlyActivePlanets()
        {
            // Arrange
            var allPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) },
                { PlanetType.Tatooine, new PlanetAttribute("Tatooine", "Tatooine - ", "Tatooine_Orbit", "TATOOINE_LANDING", 400, true) },
                { PlanetType.Invalid, new PlanetAttribute("Invalid", "-- Invalid --", "", "", 0, false) }
            };

            var activePlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, allPlanets[PlanetType.Viscara] },
                { PlanetType.Tatooine, allPlanets[PlanetType.Tatooine] }
            };

            SetupMockCache(allPlanets);
            _mockEnumCache.GetFilteredCache("Active").Returns(activePlanets);

            // Act
            var result = _service.GetActivePlanets();

            // Assert
            Assert.That(result, Is.EqualTo(activePlanets));
        }

        [Test]
        public void HasPlanet_WithExistingPlanet_ShouldReturnTrue()
        {
            // Arrange
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) }
            };

            SetupMockCache(mockPlanets);

            // Act
            var result = _service.HasPlanet(PlanetType.Viscara);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasPlanet_WithNonExistingPlanet_ShouldReturnFalse()
        {
            // Arrange
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>();
            SetupMockCache(mockPlanets);

            // Act
            var result = _service.HasPlanet(PlanetType.Viscara);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void HasPlanet_WithNullCache_ShouldReturnFalse()
        {
            // Arrange - Don't initialize cache, so _planetCache is null

            // Act
            var result = _service.HasPlanet(PlanetType.Viscara);

            // Assert
            Assert.That(result, Is.False);
        }

        private void SetupMockCache(Dictionary<PlanetType, PlanetAttribute> planets)
        {
            var mockCacheBuilder = Substitute.For<IEnumCacheBuilder<PlanetType, PlanetAttribute>>();
            var mockFilteredCache = Substitute.For<IEnumCacheBuilder<PlanetType, PlanetAttribute>>();
            
            _mockCacheService.BuildEnumCache<PlanetType, PlanetAttribute>().Returns(mockCacheBuilder);
            mockCacheBuilder.WithAllItems().Returns(mockCacheBuilder);
            mockCacheBuilder.WithFilteredCache("Active", Arg.Any<Func<PlanetAttribute, bool>>()).Returns(mockFilteredCache);
            mockFilteredCache.Build().Returns(_mockEnumCache);
            _mockEnumCache.AllItems.Returns(planets);

            _service.InitializeCache();
        }
    }
}
