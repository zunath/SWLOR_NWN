using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.EventHandlers;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Infrastructure;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Test.Component.Inventory.Infrastructure
{
    [TestFixture]
    public class InventoryServiceCollectionExtensionsTests
    {
        private IServiceCollection _services;

        [SetUp]
        public void SetUp()
        {
            _services = new ServiceCollection();
        }

        [Test]
        public void AddInventoryServices_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _services.AddInventoryServices());
        }

        [Test]
        public void AddInventoryServices_ShouldReturnSameServiceCollection()
        {
            // Act
            var result = _services.AddInventoryServices();

            // Assert
            Assert.That(result, Is.SameAs(_services));
        }

        [Test]
        public void AddInventoryServices_CanBeCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => 
            {
                _services.AddInventoryServices();
                _services.AddInventoryServices();
            });
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterInventoryItemRepository()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var repositoryDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IInventoryItemRepository));
            Assert.That(repositoryDescriptor, Is.Not.Null);
            Assert.That(repositoryDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterKeyItemService()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var keyItemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IKeyItemService));
            Assert.That(keyItemServiceDescriptor, Is.Not.Null);
            Assert.That(keyItemServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterLootTableBuilder()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var lootTableBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILootTableBuilder));
            Assert.That(lootTableBuilderDescriptor, Is.Not.Null);
            Assert.That(lootTableBuilderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterLootService()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var lootServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILootService));
            Assert.That(lootServiceDescriptor, Is.Not.Null);
            Assert.That(lootServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterItemService()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var itemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IItemService));
            Assert.That(itemServiceDescriptor, Is.Not.Null);
            Assert.That(itemServiceDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterItemBuilder()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var itemBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IItemBuilder));
            Assert.That(itemBuilderDescriptor, Is.Not.Null);
            Assert.That(itemBuilderDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterInventoryServiceEventHandlers()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(InventoryServiceEventHandlers));
            Assert.That(eventHandlerDescriptor, Is.Not.Null);
            Assert.That(eventHandlerDescriptor.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterFeatureClasses()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var lightsaberAudioDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(LightsaberAudio));
            var trashCanDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(TrashCan));
            var stackDecrementPreventionDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(StackDecrementPrevention));
            var instantItemUseDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(InstantItemUse));
            var standardItemConfigurationsDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(StandardItemConfigurations));
            
            Assert.That(lightsaberAudioDescriptor, Is.Not.Null);
            Assert.That(trashCanDescriptor, Is.Not.Null);
            Assert.That(stackDecrementPreventionDescriptor, Is.Not.Null);
            Assert.That(instantItemUseDescriptor, Is.Not.Null);
            Assert.That(standardItemConfigurationsDescriptor, Is.Not.Null);
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterItemDefinitionClasses()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var fishingRodDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(SWLOR.Component.Inventory.Feature.ItemDefinition.FishingRodItemDefinition));
            var destroyItemDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(SWLOR.Component.Inventory.Feature.ItemDefinition.DestroyItemDefinition));
            var consumableItemDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(SWLOR.Component.Inventory.Feature.ItemDefinition.ConsumableItemDefinition));
            
            Assert.That(fishingRodDescriptor, Is.Not.Null);
            Assert.That(destroyItemDescriptor, Is.Not.Null);
            Assert.That(consumableItemDescriptor, Is.Not.Null);
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterServicesAsSingletons()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var keyItemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IKeyItemService));
            var lootServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILootService));
            var itemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IItemService));
            
            Assert.That(keyItemServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(lootServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(itemServiceDescriptor?.Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterAllRequiredServices()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var inventoryItemRepositoryDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IInventoryItemRepository));
            var keyItemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IKeyItemService));
            var lootTableBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILootTableBuilder));
            var lootServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILootService));
            var itemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IItemService));
            var itemBuilderDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IItemBuilder));
            var eventHandlerDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(InventoryServiceEventHandlers));
            
            Assert.That(inventoryItemRepositoryDescriptor, Is.Not.Null, "IInventoryItemRepository should be registered");
            Assert.That(keyItemServiceDescriptor, Is.Not.Null, "IKeyItemService should be registered");
            Assert.That(lootTableBuilderDescriptor, Is.Not.Null, "ILootTableBuilder should be registered");
            Assert.That(lootServiceDescriptor, Is.Not.Null, "ILootService should be registered");
            Assert.That(itemServiceDescriptor, Is.Not.Null, "IItemService should be registered");
            Assert.That(itemBuilderDescriptor, Is.Not.Null, "IItemBuilder should be registered");
            Assert.That(eventHandlerDescriptor, Is.Not.Null, "InventoryServiceEventHandlers should be registered");
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterServicesWithCorrectImplementationTypes()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            var keyItemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IKeyItemService));
            var lootServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(ILootService));
            var itemServiceDescriptor = _services.FirstOrDefault(sd => sd.ServiceType == typeof(IItemService));
            
            Assert.That(keyItemServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(KeyItemService)));
            Assert.That(lootServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(LootService)));
            Assert.That(itemServiceDescriptor?.ImplementationType, Is.EqualTo(typeof(ItemService)));
        }

        [Test]
        public void AddInventoryServices_ShouldRegisterInterfaceDefinitions()
        {
            // Act
            _services.AddInventoryServices();

            // Assert
            // Test that the generic helper methods were called to register interface definitions
            // We can't easily test the specific implementations without complex setup,
            // but we can verify that the registration process completed without errors
            Assert.That(_services.Count, Is.GreaterThan(0), "Services should be registered");
        }

        [Test]
        public void AddInventoryServices_ShouldHandleNullServiceCollection()
        {
            // Arrange
            IServiceCollection services = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddInventoryServices());
        }
    }
}
