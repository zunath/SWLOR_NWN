using NSubstitute;
using SWLOR.Component.World.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Test.Shared;

namespace SWLOR.Test.Component.World.Service
{
    [TestFixture]
    public class VisibilityObjectCacheServiceTests : TestBase
    {
        private ILogger _mockLogger;
        private VisibilityObjectCacheService _service;

        [SetUp]
        public void SetUp()
        {
            // Initialize the mock NWScript service for all tests
            InitializeMockNWScript();
            
            _mockLogger = Substitute.For<ILogger>();
            _service = new VisibilityObjectCacheService(_mockLogger);
        }

        [Test]
        public void LoadVisibilityObjects_ShouldLoadObjectsFromAreas()
        {
            // Arrange
            // Note: This test would require mocking NWScript functions, which is complex
            // In a real implementation, you'd need to mock the static NWScript methods
            // For now, this is a placeholder test structure

            // Act
            _service.LoadVisibilityObjects();

            // Assert
            // Verify that objects were loaded into the cache
            // This would require mocking the NWScript area iteration methods
        }

        [Test]
        public void GetVisibilityObject_WithValidId_ShouldReturnObject()
        {
            // Arrange
            var visibilityObjectId = "test-object-1";
            var expectedObjectId = 12345u;

            // Manually add to cache for testing
            var cacheField = typeof(VisibilityObjectCacheService)
                .GetField("_visibilityObjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cache = (Dictionary<string, uint>)cacheField.GetValue(_service);
            cache[visibilityObjectId] = expectedObjectId;

            // Act
            var result = _service.GetVisibilityObject(visibilityObjectId);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(expectedObjectId));
        }

        [Test]
        public void GetVisibilityObject_WithInvalidId_ShouldReturnNull()
        {
            // Arrange
            var visibilityObjectId = "non-existent-object";

            // Act
            var result = _service.GetVisibilityObject(visibilityObjectId);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void GetDefaultHiddenObjects_ShouldReturnHiddenObjects()
        {
            // Arrange
            var hiddenObject1 = 12345u;
            var hiddenObject2 = 67890u;

            // Manually add to cache for testing
            var cacheField = typeof(VisibilityObjectCacheService)
                .GetField("_defaultHiddenObjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cache = (List<uint>)cacheField.GetValue(_service);
            cache.Add(hiddenObject1);
            cache.Add(hiddenObject2);

            // Act
            var result = _service.GetDefaultHiddenObjects();

            // Assert
            Assert.That(result.Count(), Is.EqualTo(2));
            Assert.That(result, Contains.Item(hiddenObject1));
            Assert.That(result, Contains.Item(hiddenObject2));
        }

        [Test]
        public void HasVisibilityObject_WithValidId_ShouldReturnTrue()
        {
            // Arrange
            var visibilityObjectId = "test-object-1";
            var objectId = 12345u;

            // Manually add to cache for testing
            var cacheField = typeof(VisibilityObjectCacheService)
                .GetField("_visibilityObjects", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cache = (Dictionary<string, uint>)cacheField.GetValue(_service);
            cache[visibilityObjectId] = objectId;

            // Act
            var result = _service.HasVisibilityObject(visibilityObjectId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasVisibilityObject_WithInvalidId_ShouldReturnFalse()
        {
            // Arrange
            var visibilityObjectId = "non-existent-object";

            // Act
            var result = _service.HasVisibilityObject(visibilityObjectId);

            // Assert
            Assert.That(result, Is.False);
        }
    }
}
