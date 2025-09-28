using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.World.Service
{
    [TestFixture]
    public class PlanetServiceTests : TestBase
    {
        private IPlanetCacheService _mockPlanetCacheService;
        private IPlanetAreaService _mockPlanetAreaService;
        private PlanetService _service;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service for all tests
            InitializeMockNWScript();
            
            _mockPlanetCacheService = Substitute.For<IPlanetCacheService>();
            _mockPlanetAreaService = Substitute.For<IPlanetAreaService>();
            _service = new PlanetService(_mockPlanetCacheService, _mockPlanetAreaService);
        }

        [Test]
        public void CacheData_ShouldInitializeCacheAndRegisterAreaPlanetIds()
        {
            // Act
            _service.CacheData();

            // Assert
            _mockPlanetCacheService.Received(1).InitializeCache();
            _mockPlanetAreaService.Received(1).RegisterAreaPlanetIds();
        }

        [Test]
        public void GetPlanetType_ShouldDelegateToPlanetAreaService()
        {
            // Arrange
            var area = 1001u;
            var expectedPlanetType = PlanetType.Viscara;
            _mockPlanetAreaService.GetPlanetType(area).Returns(expectedPlanetType);

            // Act
            var result = _service.GetPlanetType(area);

            // Assert
            _mockPlanetAreaService.Received(1).GetPlanetType(area);
            Assert.That(result, Is.EqualTo(expectedPlanetType));
        }

        [Test]
        public void GetPlanetByType_ShouldDelegateToPlanetCacheService()
        {
            // Arrange
            var planetType = PlanetType.Viscara;
            var expectedPlanet = new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true);
            _mockPlanetCacheService.GetPlanetByType(planetType).Returns(expectedPlanet);

            // Act
            var result = _service.GetPlanetByType(planetType);

            // Assert
            _mockPlanetCacheService.Received(1).GetPlanetByType(planetType);
            Assert.That(result, Is.EqualTo(expectedPlanet));
        }

        [Test]
        public void GetAllPlanets_ShouldDelegateToPlanetCacheService()
        {
            // Arrange
            var expectedPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) },
                { PlanetType.Tatooine, new PlanetAttribute("Tatooine", "Tatooine - ", "Tatooine_Orbit", "TATOOINE_LANDING", 400, true) }
            };

            _mockPlanetCacheService.GetAllPlanets().Returns(expectedPlanets);

            // Act
            var result = _service.GetAllPlanets();

            // Assert
            _mockPlanetCacheService.Received(1).GetAllPlanets();
            Assert.That(result, Is.EqualTo(expectedPlanets));
        }

        [Test]
        public void GetPlanetByType_WithExceptionFromCacheService_ShouldPropagateException()
        {
            // Arrange
            var planetType = PlanetType.Viscara;
            var expectedException = new KeyNotFoundException("Planet not found");
            _mockPlanetCacheService.When(x => x.GetPlanetByType(planetType)).Throw(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<KeyNotFoundException>(() => _service.GetPlanetByType(planetType));
            Assert.That(thrownException, Is.EqualTo(expectedException));
        }

        [Test]
        public void GetPlanetType_WithExceptionFromAreaService_ShouldPropagateException()
        {
            // Arrange
            var area = 1001u;
            var expectedException = new InvalidOperationException("Area service error");
            _mockPlanetAreaService.When(x => x.GetPlanetType(area)).Throw(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => _service.GetPlanetType(area));
            Assert.That(thrownException, Is.EqualTo(expectedException));
        }

        [Test]
        public void GetAllPlanets_WithExceptionFromCacheService_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Cache service error");
            _mockPlanetCacheService.When(x => x.GetAllPlanets()).Throw(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => _service.GetAllPlanets());
            Assert.That(thrownException, Is.EqualTo(expectedException));
        }

        [Test]
        public void CacheData_WithExceptionFromCacheService_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Cache initialization error");
            _mockPlanetCacheService.When(x => x.InitializeCache()).Throw(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => _service.CacheData());
            Assert.That(thrownException, Is.EqualTo(expectedException));
        }

        [Test]
        public void CacheData_WithExceptionFromAreaService_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Area registration error");
            _mockPlanetAreaService.When(x => x.RegisterAreaPlanetIds()).Throw(expectedException);

            // Act & Assert
            var thrownException = Assert.Throws<InvalidOperationException>(() => _service.CacheData());
            Assert.That(thrownException, Is.EqualTo(expectedException));
        }
    }
}
