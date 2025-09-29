using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Domain.World.Enums;

namespace SWLOR.Test.Shared.Caching.Service
{
    [TestFixture]
    public class GenericCacheServiceTests : TestBase
    {
        private IServiceProvider _mockServiceProvider;
        private GenericCacheService _service;

        [SetUp]
        public void SetUp()
        {
            _mockServiceProvider = Substitute.For<IServiceProvider>();
            _service = new GenericCacheService(_mockServiceProvider);
        }

        [Test]
        public void Constructor_WithNullServiceProvider_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new GenericCacheService(null));
        }

        [Test]
        public void Constructor_WithServiceProvider_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new GenericCacheService(_mockServiceProvider));
        }

        [Test]
        public void BuildEnumCache_ShouldReturnEnumCacheBuilder()
        {
            // Act
            var result = _service.BuildEnumCache<PlanetType, PlanetAttribute>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<IEnumCacheBuilder<PlanetType, PlanetAttribute>>());
        }

        [Test]
        public void BuildInterfaceCache_ShouldReturnInterfaceCacheBuilder()
        {
            // Act
            var result = _service.BuildInterfaceCache<ITestInterface, string, string>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<IInterfaceCacheBuilder<ITestInterface, string, string>>());
        }

        [Test]
        public void BuildInterfaceCache_WithNullServiceProvider_ShouldReturnInterfaceCacheBuilder()
        {
            // Arrange
            var serviceWithNullProvider = new GenericCacheService(null);

            // Act
            var result = serviceWithNullProvider.BuildInterfaceCache<ITestInterface, string, string>();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<IInterfaceCacheBuilder<ITestInterface, string, string>>());
        }
    }

}
