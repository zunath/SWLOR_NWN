using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.EventHandlers;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Repository;
using SWLOR.Component.Inventory.Service;
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
            services.AddSingleton<Feature.ItemDefinition.FishingRodItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.DestroyItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.ConsumableItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.TomeItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.DroidControlItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.KeyItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.HarvesterItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.RecipeItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.SpeederItemDefinition>();
            services.AddSingleton<Feature.ItemDefinition.SaberUpgradeItemDefinition>();
            
            services.RegisterInterfaceImplementationsWithInterface<ISnippetListDefinition>();
            services.RegisterInterfaceImplementations<ILootTableDefinition>();
            services.RegisterInterfaceImplementations<IItemListDefinition>();

            return services;
        }

    }
}
