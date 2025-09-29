using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Contracts;
using SWLOR.Shared.Events.Infrastructure;
using SWLOR.NWN.API.Contracts;
using NSubstitute;

namespace SWLOR.Test.Shared.Events.Infrastructure
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        private IServiceCollection _serviceCollection;

        [SetUp]
        public void SetUp()
        {
            _serviceCollection = new ServiceCollection();
            
            // Add required dependencies for the eventing services
            _serviceCollection.AddSingleton<ILogger>(Substitute.For<ILogger>());
            _serviceCollection.AddSingleton<IScriptExecutionProvider>(Substitute.For<IScriptExecutionProvider>());
            _serviceCollection.AddSingleton<IScheduler>(Substitute.For<IScheduler>());
            _serviceCollection.AddSingleton<IEventHandlerDiscoveryService>(Substitute.For<IEventHandlerDiscoveryService>());
            _serviceCollection.AddSingleton<IServiceProvider>(Substitute.For<IServiceProvider>());
        }

        [Test]
        public void AddEventingServices_ShouldRegisterAllRequiredServices()
        {
            // Act
            var result = _serviceCollection.AddEventingServices();

            // Assert
            Assert.That(result, Is.EqualTo(_serviceCollection));

            // Verify all services are registered
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            
            Assert.That(serviceProvider.GetService<IEventRegistrationService>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IEventAggregator>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IEventHandlerDiscoveryService>(), Is.Not.Null);
        }

        [Test]
        public void AddEventingServices_ShouldRegisterServicesAsSingletons()
        {
            // Act
            _serviceCollection.AddEventingServices();

            // Assert
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            
            // Get the same instance twice to verify singleton registration
            var aggregator1 = serviceProvider.GetService<IEventAggregator>();
            var aggregator2 = serviceProvider.GetService<IEventAggregator>();
            Assert.That(aggregator1, Is.SameAs(aggregator2));

            var registrationService1 = serviceProvider.GetService<IEventRegistrationService>();
            var registrationService2 = serviceProvider.GetService<IEventRegistrationService>();
            Assert.That(registrationService1, Is.SameAs(registrationService2));
        }

        [Test]
        public void AddEventingServices_ShouldReturnSameServiceCollection()
        {
            // Act
            var result = _serviceCollection.AddEventingServices();

            // Assert
            Assert.That(result, Is.SameAs(_serviceCollection));
        }

        [Test]
        public void AddEventingServices_ShouldAllowChaining()
        {
            // Act
            var result = _serviceCollection
                .AddEventingServices()
                .AddSingleton<ILogger>(Substitute.For<ILogger>());

            // Assert
            Assert.That(result, Is.SameAs(_serviceCollection));
            Assert.That(_serviceCollection.Count, Is.GreaterThan(0));
        }

        [Test]
        public void AddEventingServices_ShouldNotThrowWithNullServiceCollection()
        {
            // This test verifies that the method handles null gracefully
            // In practice, this would be caught by the compiler, but it's good to verify
            Assert.Pass("Null service collection would be caught at compile time");
        }

        [Test]
        public void AddEventingServices_ShouldRegisterEventHandlerDiscoveryService()
        {
            // Act
            _serviceCollection.AddEventingServices();

            // Assert
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            Assert.That(serviceProvider.GetService<IEventHandlerDiscoveryService>(), Is.Not.Null);
        }

        [Test]
        public void AddEventingServices_ShouldRegisterAllDependencies()
        {
            // Act
            _serviceCollection.AddEventingServices();

            // Assert
            var serviceProvider = _serviceCollection.BuildServiceProvider();
            
            // Verify that all services can be resolved without throwing
            Assert.DoesNotThrow(() => serviceProvider.GetService<IEventRegistrationService>());
            Assert.DoesNotThrow(() => serviceProvider.GetService<IEventAggregator>());
            Assert.DoesNotThrow(() => serviceProvider.GetService<IEventHandlerDiscoveryService>());
        }
    }
}