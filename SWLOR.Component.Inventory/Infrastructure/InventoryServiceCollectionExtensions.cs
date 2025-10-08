using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.Definitions.ItemDefinition;
using SWLOR.Component.Inventory.EventHandlers;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Repository;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Inventory.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Inventory-related services in the dependency injection container.
    /// </summary>
    public static class InventoryServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Inventory services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddInventoryServices(this IServiceCollection services)
        {
            services.AddSingleton<IInventoryItemRepository, InventoryItemRepository>();
            services.AddSingleton<IWeaponStatService, WeaponStatService>();
            services.AddSingleton<IKeyItemService, KeyItemService>();
            services.AddSingleton<ILootTableBuilder, LootTableBuilder>();
            services.AddSingleton<ILootService, LootService>();
            services.AddSingleton<IItemService, ItemService>();
            services.AddSingleton<IItemBuilder, ItemBuilder>();
            services.AddSingleton<InventoryEventHandlers>();
            services.AddSingleton<InventoryServiceEventHandlers>();
            services.AddSingleton<LightsaberAudio>();
            services.AddSingleton<TrashCan>();
            services.AddSingleton<StackDecrementPrevention>();
            services.AddSingleton<InstantItemUse>();
            services.AddSingleton<StandardItemConfigurations>();
            services.AddSingleton<FishingRodItemDefinition>();
            services.AddSingleton<DestroyItemDefinition>();
            services.AddSingleton<ConsumableItemDefinition>();
            services.AddSingleton<TomeItemDefinition>();
            services.AddSingleton<DroidControlItemDefinition>();
            services.AddSingleton<KeyItemDefinition>();
            services.AddSingleton<HarvesterItemDefinition>();
            services.AddSingleton<RecipeItemDefinition>();
            services.AddSingleton<SpeederItemDefinition>();
            services.AddSingleton<SaberUpgradeItemDefinition>();
            
            services.RegisterInterfaceImplementationsWithInterface<ISnippetListDefinition>();
            services.RegisterInterfaceImplementations<ILootTableDefinition>();
            services.RegisterInterfaceImplementations<IItemListDefinition>();

            return services;
        }

    }
}
