using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Caching.Contracts;
using SWLOR.Shared.Caching.Extensions;
using SWLOR.Shared.Caching.Service;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Domain.Properties.Contracts;

namespace SWLOR.Test.Shared.Caching.Extensions
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests : TestBase
    {
        private IServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
        }

        [Test]
        public void AddCacheServices_ShouldRegisterAllServices()
        {
            // Arrange - Add required dependencies
            _services.AddSingleton(Substitute.For<IDatabaseService>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());
            _services.AddSingleton(Substitute.For<IPropertyService>());
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IUtilPluginService>());

            // Act
            var result = _services.AddCacheServices();

            // Assert
            Assert.That(result, Is.EqualTo(_services));

            // Verify all services are registered
            var serviceProvider = _services.BuildServiceProvider();

            // Core services
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IGenericCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IItemCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IPortraitCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<ISoundSetCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IModuleCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IAreaCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<ISongCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<IPlanetCacheService>());
            Assert.DoesNotThrow(() => serviceProvider.GetRequiredService<ISpawnCacheService>());

            // Note: CachingEventHandlers is internal and not directly testable
        }

        [Test]
        public void AddCacheServices_ShouldRegisterServicesAsSingletons()
        {
            // Arrange - Add required dependencies
            _services.AddSingleton(Substitute.For<IDatabaseService>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());
            _services.AddSingleton(Substitute.For<IPropertyService>());
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IUtilPluginService>());

            // Act
            _services.AddCacheServices();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();

            // Get the same service twice and verify they're the same instance (singleton)
            var service1 = serviceProvider.GetRequiredService<IGenericCacheService>();
            var service2 = serviceProvider.GetRequiredService<IGenericCacheService>();
            Assert.That(service1, Is.EqualTo(service2));

            var itemService1 = serviceProvider.GetRequiredService<IItemCacheService>();
            var itemService2 = serviceProvider.GetRequiredService<IItemCacheService>();
            Assert.That(itemService1, Is.EqualTo(itemService2));
        }

        [Test]
        public void AddCacheServices_ShouldReturnSameServiceCollection()
        {
            // Act
            var result = _services.AddCacheServices();

            // Assert
            Assert.That(result, Is.EqualTo(_services));
        }

        [Test]
        public void AddCacheServices_ShouldRegisterGenericCacheServiceWithServiceProvider()
        {
            // Act
            _services.AddCacheServices();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var genericCacheService = serviceProvider.GetRequiredService<IGenericCacheService>();
            
            Assert.That(genericCacheService, Is.Not.Null);
            Assert.That(genericCacheService, Is.InstanceOf<GenericCacheService>());
        }

        [Test]
        public void AddCacheServices_ShouldRegisterAllCacheServices()
        {
            // Arrange - Add required dependencies
            _services.AddSingleton(Substitute.For<IDatabaseService>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());
            _services.AddSingleton(Substitute.For<IPropertyService>());
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IUtilPluginService>());

            // Act
            _services.AddCacheServices();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();

            // Verify all cache services are registered with correct implementations
            Assert.That(serviceProvider.GetRequiredService<IItemCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<IPortraitCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<ISoundSetCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<IModuleCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<IAreaCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<ISongCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<IPlanetCacheService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetRequiredService<ISpawnCacheService>(), Is.Not.Null);
        }
    }
}
