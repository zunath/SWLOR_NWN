using NSubstitute;
using NUnit.Framework;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.World.Service
{
    [TestFixture]
    public class PlanetAreaServiceTests : TestBase
    {
        private IPlanetCacheService _mockPlanetCacheService;
        private PlanetAreaService _service;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service for all tests
            InitializeMockNWScript();
            
            // Reset mock state to ensure clean test environment
            var mockService = GetMockService();
            mockService.ResetMockState();
            
            _mockPlanetCacheService = Substitute.For<IPlanetCacheService>();
            _service = new PlanetAreaService(_mockPlanetCacheService);
        }

        [Test]
        public void RegisterAreaPlanetIds_WithMatchingAreas_ShouldSetPlanetTypeId()
        {
            // Arrange
            var mockService = GetMockService();
            mockService.ResetMockState(); // Reset mock state for this test
            
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) },
                { PlanetType.Tatooine, new PlanetAttribute("Tatooine", "Tatooine - ", "Tatooine_Orbit", "TATOOINE_LANDING", 400, true) }
            };

            _mockPlanetCacheService.GetAllPlanets().Returns(mockPlanets);
            
            // Create areas and get their IDs
            var area1 = mockService.CreateArea("", "", "Viscara - Test Area");
            var area2 = mockService.CreateArea("", "", "Tatooine - Desert Area");
            var area3 = mockService.CreateArea("", "", "Unknown - Some Area");
            
            // Set names for the areas
            mockService.SetName(area1, "Viscara - Test Area");
            mockService.SetName(area2, "Tatooine - Desert Area");
            mockService.SetName(area3, "Unknown - Some Area");

            // Act
            _service.RegisterAreaPlanetIds();

            // Assert
            var planetTypeId1 = mockService.GetLocalInt(area1, "PLANET_TYPE_ID");
            var planetTypeId2 = mockService.GetLocalInt(area2, "PLANET_TYPE_ID");
            var planetTypeId3 = mockService.GetLocalInt(area3, "PLANET_TYPE_ID");

            Assert.That(planetTypeId1, Is.EqualTo((int)PlanetType.Viscara));
            Assert.That(planetTypeId2, Is.EqualTo((int)PlanetType.Tatooine));
            Assert.That(planetTypeId3, Is.EqualTo(0)); // No matching prefix
        }

        [Test]
        public void RegisterAreaPlanetIds_WithNoMatchingAreas_ShouldNotSetAnyPlanetTypeId()
        {
            // Arrange
            var mockService = GetMockService();
            mockService.ResetMockState(); // Reset mock state for this test
            
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { PlanetType.Viscara, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) }
            };

            _mockPlanetCacheService.GetAllPlanets().Returns(mockPlanets);
            
            // Create areas with non-matching names
            var area1 = mockService.CreateArea("", "", "Some Other Area");
            var area2 = mockService.CreateArea("", "", "Another Area");
            
            // Set names for the areas
            mockService.SetName(area1, "Some Other Area");
            mockService.SetName(area2, "Another Area");

            // Act
            _service.RegisterAreaPlanetIds();

            // Assert
            var planetTypeId1 = mockService.GetLocalInt(area1, "PLANET_TYPE_ID");
            var planetTypeId2 = mockService.GetLocalInt(area2, "PLANET_TYPE_ID");

            Assert.That(planetTypeId1, Is.EqualTo(0));
            Assert.That(planetTypeId2, Is.EqualTo(0));
        }

        [Test]
        public void GetPlanetType_WithValidArea_ShouldReturnPlanetType()
        {
            // Arrange
            var area = 1001u;
            var expectedPlanetType = PlanetType.Viscara;

            var mockService = GetMockService();
            mockService.SetLocalInt(area, "PLANET_TYPE_ID", (int)expectedPlanetType);

            // Act
            var result = _service.GetPlanetType(area);

            // Assert
            Assert.That(result, Is.EqualTo(expectedPlanetType));
        }

        [Test]
        public void GetPlanetType_WithInvalidArea_ShouldReturnInvalid()
        {
            // Arrange
            var area = 1001u;
            var mockService = GetMockService();
            mockService.SetLocalInt(area, "PLANET_TYPE_ID", (int)PlanetType.Invalid);

            // Act
            var result = _service.GetPlanetType(area);

            // Assert
            Assert.That(result, Is.EqualTo(PlanetType.Invalid));
        }

        [Test]
        public void IsAreaOnPlanet_WithMatchingPlanetType_ShouldReturnTrue()
        {
            // Arrange
            var area = 1001u;
            var planetType = PlanetType.Viscara;

            var mockService = GetMockService();
            mockService.SetLocalInt(area, "PLANET_TYPE_ID", (int)planetType);

            // Act
            var result = _service.IsAreaOnPlanet(area, planetType);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsAreaOnPlanet_WithNonMatchingPlanetType_ShouldReturnFalse()
        {
            // Arrange
            var area = 1001u;
            var planetType = PlanetType.Viscara;
            var otherPlanetType = PlanetType.Tatooine;

            var mockService = GetMockService();
            mockService.SetLocalInt(area, "PLANET_TYPE_ID", (int)planetType);

            // Act
            var result = _service.IsAreaOnPlanet(area, otherPlanetType);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void GetAreasForPlanet_WithMatchingAreas_ShouldReturnAreaList()
        {
            // Arrange
            var planetType = PlanetType.Viscara;
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { planetType, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) }
            };

            _mockPlanetCacheService.GetAllPlanets().Returns(mockPlanets);

            var mockService = GetMockService();
            
            // Create areas and get their IDs
            var area1 = mockService.CreateArea("", "", "Viscara - Test Area");
            var area2 = mockService.CreateArea("", "", "Viscara - Another Area");
            var area3 = mockService.CreateArea("", "", "Tatooine - Desert Area");
            
            // Set names for the areas
            mockService.SetName(area1, "Viscara - Test Area");
            mockService.SetName(area2, "Viscara - Another Area");
            mockService.SetName(area3, "Tatooine - Desert Area");

            // Set the planet type IDs for the areas
            mockService.SetLocalInt(area1, "PLANET_TYPE_ID", (int)planetType);
            mockService.SetLocalInt(area2, "PLANET_TYPE_ID", (int)planetType);
            mockService.SetLocalInt(area3, "PLANET_TYPE_ID", (int)PlanetType.Tatooine);

            // Act
            var result = _service.GetAreasForPlanet(planetType);

            // Assert
            Assert.That(result, Contains.Item(area1));
            Assert.That(result, Contains.Item(area2));
            Assert.That(result, Does.Not.Contain(area3));
            Assert.That(result.Count, Is.EqualTo(2));
        }

        [Test]
        public void GetAreasForPlanet_WithNonExistentPlanet_ShouldReturnEmptyList()
        {
            // Arrange
            var planetType = PlanetType.Viscara;
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>();

            _mockPlanetCacheService.GetAllPlanets().Returns(mockPlanets);

            var mockService = GetMockService();
            
            // Create area and get its ID
            var area1 = mockService.CreateArea("", "", "Viscara - Test Area");
            mockService.SetName(area1, "Viscara - Test Area");

            // Act
            var result = _service.GetAreasForPlanet(planetType);

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetAreasForPlanet_WithNoMatchingAreas_ShouldReturnEmptyList()
        {
            // Arrange
            var planetType = PlanetType.Viscara;
            var mockPlanets = new Dictionary<PlanetType, PlanetAttribute>
            {
                { planetType, new PlanetAttribute("Viscara", "Viscara - ", "Viscara_Orbit", "VISCARA_LANDING", 100, true) }
            };

            _mockPlanetCacheService.GetAllPlanets().Returns(mockPlanets);

            var mockService = GetMockService();
            
            // Create areas with non-matching names
            var area1 = mockService.CreateArea("", "", "Tatooine - Desert Area");
            var area2 = mockService.CreateArea("", "", "Some Other Area");
            
            // Set names for the areas
            mockService.SetName(area1, "Tatooine - Desert Area");
            mockService.SetName(area2, "Some Other Area");

            // Act
            var result = _service.GetAreasForPlanet(planetType);

            // Assert
            Assert.That(result, Is.Empty);
        }
    }
}
