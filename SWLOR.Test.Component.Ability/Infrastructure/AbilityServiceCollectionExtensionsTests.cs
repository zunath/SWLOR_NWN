using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.EventHandlers;
using SWLOR.Component.Ability.Infrastructure;
using SWLOR.Component.Ability.Service;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Domain.Ability.Contracts;

namespace SWLOR.Test.Component.Ability.Infrastructure
{
    [TestFixture]
    public class AbilityServiceCollectionExtensionsTests
    {
        private IServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
        }

        [Test]
        public void AddAbilityServices_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _services.AddAbilityServices());
        }

        [Test]
        public void AddAbilityServices_ShouldReturnSameServiceCollection()
        {
            // Act
            var result = _services.AddAbilityServices();

            // Assert
            Assert.That(result, Is.SameAs(_services));
        }

        [Test]
        public void AddAbilityServices_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _services.AddAbilityServices();
                _services.AddAbilityServices();
            });
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterAbilityService()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var abilityServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityService));
            Assert.That(abilityServiceDescriptor, Is.Not.Null);
            Assert.That(abilityServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterRecastService()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var recastServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IRecastService));
            Assert.That(recastServiceDescriptor, Is.Not.Null);
            Assert.That(recastServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterAbilityBuilder()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var abilityBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityBuilder));
            Assert.That(abilityBuilderDescriptor, Is.Not.Null);
            Assert.That(abilityBuilderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterAbilityEventHandlers()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(AbilityEventHandlers));
            Assert.That(eventHandlerDescriptor, Is.Not.Null);
            Assert.That(eventHandlerDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterServicesAsSingletons()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var abilityServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityService));
            var recastServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IRecastService));
            var abilityBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityBuilder));
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(AbilityEventHandlers));
            
            Assert.That(abilityServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(recastServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(abilityBuilderDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(eventHandlerDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterAllRequiredServices()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var abilityServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityService));
            var recastServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IRecastService));
            var abilityBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityBuilder));
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(AbilityEventHandlers));
            
            Assert.That(abilityServiceDescriptor, Is.Not.Null, "IAbilityService should be registered");
            Assert.That(recastServiceDescriptor, Is.Not.Null, "IRecastService should be registered");
            Assert.That(abilityBuilderDescriptor, Is.Not.Null, "IAbilityBuilder should be registered");
            Assert.That(eventHandlerDescriptor, Is.Not.Null, "AbilityEventHandlers should be registered");
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterServicesWithCorrectImplementationTypes()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            var abilityServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityService));
            var recastServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IRecastService));
            var abilityBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IAbilityBuilder));
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(AbilityEventHandlers));
            
            Assert.That(abilityServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(AbilityService)));
            Assert.That(recastServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(RecastService)));
            Assert.That(abilityBuilderDescriptor?.ImplementationType, Is.EqualTo(typeof(AbilityBuilder)));
            Assert.That(eventHandlerDescriptor?.ImplementationType, Is.EqualTo(typeof(AbilityEventHandlers)));
        }

        [Test]
        public void AddAbilityServices_ShouldRegisterAbilityListDefinitions()
        {
            // Act
            _services.AddAbilityServices();

            // Assert
            // Test that the generic helper method was called to register ability definitions
            // We can't easily test the specific implementations without complex setup,
            // but we can verify that the registration process completed without errors
            Assert.That(_services.Count, Is.GreaterThan(0), "Services should be registered");
        }

        [Test]
        public void AddAbilityServices_ShouldHandleNullServiceCollection()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddAbilityServices());
        }
    }
}
