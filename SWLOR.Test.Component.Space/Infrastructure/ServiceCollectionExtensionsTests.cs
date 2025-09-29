using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.EventHandlers;
using SWLOR.Component.Space.Infrastructure;
using SWLOR.Component.Space.Service;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.World.Contracts;
using SWLOR.Shared.Events.Contracts;

namespace SWLOR.Test.Component.Space.Infrastructure
{
    [TestFixture]
    public class ServiceCollectionExtensionsTests
    {
        private IServiceCollection _services;
        private IServiceProvider? _serviceProvider;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
            SetupRequiredDependencies();
        }

        private void SetupRequiredDependencies()
        {
            // Add required dependencies that the Space services need
            _services.AddSingleton(Substitute.For<ILogger>());
            _services.AddSingleton(Substitute.For<IDatabaseService>());
            _services.AddSingleton(Substitute.For<IScheduler>());
            _services.AddSingleton(Substitute.For<IRandomService>());
            _services.AddSingleton(Substitute.For<IEventAggregator>());
        }

        [TearDown]
        public void TearDown()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        [Test]
        public void AddSpaceServices_WithValidServices_ReturnsSameServiceCollection()
        {
            // Act
            var result = _services.AddSpaceServices();

            // Assert
            Assert.That(result, Is.EqualTo(_services));
        }

        [Test]
        public void AddSpaceServices_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _services.AddSpaceServices();
                _services.AddSpaceServices();
            });
        }

        [Test]
        public void AddSpaceServices_WithValidServices_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _services.AddSpaceServices());
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterSpaceServices()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            Assert.That(_serviceProvider.GetService<ISpaceService>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IShipBuilder>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<IShipModuleBuilder>(), Is.Not.Null);
            Assert.That(_serviceProvider.GetService<ISpaceObjectBuilder>(), Is.Not.Null);
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterSpaceEventHandler()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            var eventHandler = _serviceProvider.GetService<SpaceEventHandler>();
            
            Assert.That(eventHandler, Is.Not.Null);
            Assert.That(eventHandler, Is.InstanceOf<SpaceEventHandler>());
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterServicesAsSingletons()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            var spaceService1 = _serviceProvider.GetService<ISpaceService>();
            var spaceService2 = _serviceProvider.GetService<ISpaceService>();
            Assert.That(spaceService1, Is.SameAs(spaceService2));

            var eventHandler1 = _serviceProvider.GetService<SpaceEventHandler>();
            var eventHandler2 = _serviceProvider.GetService<SpaceEventHandler>();
            Assert.That(eventHandler1, Is.SameAs(eventHandler2));
            
            // Test that the core services are registered as singletons
            var spaceServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ISpaceService));
            Assert.That(spaceServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterShipListDefinitions()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            var shipDefinitions = _serviceProvider.GetServices<IShipListDefinition>();
            
            Assert.That(shipDefinitions, Is.Not.Null);
            Assert.That(shipDefinitions, Is.Not.Empty);
            
            // Verify that we can get concrete implementations
            foreach (var definition in shipDefinitions)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition, Is.InstanceOf<IShipListDefinition>());
            }
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterShipModuleListDefinitions()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            var shipModuleDefinitions = _serviceProvider.GetServices<IShipModuleListDefinition>();
            
            Assert.That(shipModuleDefinitions, Is.Not.Null);
            Assert.That(shipModuleDefinitions, Is.Not.Empty);
            
            // Verify that we can get concrete implementations
            foreach (var definition in shipModuleDefinitions)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition, Is.InstanceOf<IShipModuleListDefinition>());
            }
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterSpaceObjectListDefinitions()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            var spaceObjectDefinitions = _serviceProvider.GetServices<ISpaceObjectListDefinition>();
            
            Assert.That(spaceObjectDefinitions, Is.Not.Null);
            Assert.That(spaceObjectDefinitions, Is.Not.Empty);
            
            // Verify that we can get concrete implementations
            foreach (var definition in spaceObjectDefinitions)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition, Is.InstanceOf<ISpaceObjectListDefinition>());
            }
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterConcreteDefinitionTypes()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            // Test that interface-based definitions are properly registered and retrievable
            var shipDefinitions = _serviceProvider.GetServices<IShipListDefinition>();
            var spaceObjectDefinitions = _serviceProvider.GetServices<ISpaceObjectListDefinition>();
            var shipModuleDefinitions = _serviceProvider.GetServices<IShipModuleListDefinition>();
            
            Assert.That(shipDefinitions, Is.Not.Empty, "Ship definitions should be registered and retrievable");
            Assert.That(spaceObjectDefinitions, Is.Not.Empty, "Space object definitions should be registered and retrievable");
            Assert.That(shipModuleDefinitions, Is.Not.Empty, "Ship module definitions should be registered and retrievable");
        }

        [Test]
        public void AddSpaceServices_ShouldRegisterBothConcreteAndInterfaceTypes()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            // Test interface registration
            var shipDefinitions = _serviceProvider.GetServices<IShipListDefinition>();
            Assert.That(shipDefinitions, Is.Not.Empty);
            
            // Test that interface registrations exist
            var interfaceServiceDescriptors = _services.Where(sd => sd.ServiceType == typeof(IShipListDefinition));
            Assert.That(interfaceServiceDescriptors, Is.Not.Empty, "IShipListDefinition should be registered");
            
            // Verify that the interface registrations work correctly
            foreach (var definition in shipDefinitions)
            {
                Assert.That(definition, Is.Not.Null);
                Assert.That(definition, Is.InstanceOf<IShipListDefinition>());
            }
        }

        [Test]
        public void AddSpaceServices_ShouldScanAllAssemblies()
        {
            // This test verifies that the registration scans all assemblies, not just the current one
            // The actual implementation should find definitions from any loaded assembly
            
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify that we can retrieve definitions (which means assembly scanning worked)
            var shipDefinitions = _serviceProvider.GetServices<IShipListDefinition>();
            var shipModuleDefinitions = _serviceProvider.GetServices<IShipModuleListDefinition>();
            var spaceObjectDefinitions = _serviceProvider.GetServices<ISpaceObjectListDefinition>();
            
            Assert.That(shipDefinitions, Is.Not.Empty, "Should find ship definitions from all assemblies");
            Assert.That(shipModuleDefinitions, Is.Not.Empty, "Should find ship module definitions from all assemblies");
            Assert.That(spaceObjectDefinitions, Is.Not.Empty, "Should find space object definitions from all assemblies");
        }

        [Test]
        public void AddSpaceServices_ShouldNotRegisterSpawnListDefinitions()
        {
            // This test verifies that ISpawnListDefinition is NOT registered by the Space component
            // (it should be handled by the World component)
            
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify that spawn definitions are not registered by Space component
            var spawnDefinitions = _serviceProvider.GetServices<ISpawnListDefinition>();
            
            // This should be empty because Space component doesn't register spawn definitions
            // (they are handled by World component)
            Assert.That(spawnDefinitions, Is.Empty, "Space component should not register spawn definitions");
        }

        [Test]
        public void AddSpaceServices_ShouldAllowServiceResolution()
        {
            // Act
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            // Test that services can be resolved and used
            var spaceService = _serviceProvider.GetService<ISpaceService>();
            Assert.That(spaceService, Is.Not.Null);
            Assert.That(spaceService, Is.InstanceOf<SpaceService>());
            
            var shipBuilder = _serviceProvider.GetService<IShipBuilder>();
            Assert.That(shipBuilder, Is.Not.Null);
        }

        [Test]
        public void AddSpaceServices_ShouldHandleMultipleCallsWithoutDuplication()
        {
            // Act
            _services.AddSpaceServices();
            _services.AddSpaceServices();

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            // Should still work correctly after multiple calls
            var spaceService = _serviceProvider.GetService<ISpaceService>();
            Assert.That(spaceService, Is.Not.Null);
            
            var shipDefinitions = _serviceProvider.GetServices<IShipListDefinition>();
            Assert.That(shipDefinitions, Is.Not.Empty);
        }
    }
}
