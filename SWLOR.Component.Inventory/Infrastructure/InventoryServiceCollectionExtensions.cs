using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.EventHandlers;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Repository;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Repositories;

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
            // Register repositories
            services.AddSingleton<IInventoryItemRepository, InventoryItemRepository>();
            
            services.AddSingleton<IKeyItemService, KeyItemService>();
            services.AddSingleton<ILootTableBuilder, LootTableBuilder>();
            services.AddSingleton<ILootService, LootService>();
            services.AddSingleton<IItemService, ItemService>();
            services.AddSingleton<IItemBuilder, ItemBuilder>();
            services.AddSingleton<InventoryEventHandlers>();
            services.AddSingleton<InventoryServiceEventHandlers>();

            // Automatically register all snippet definition classes
            RegisterSnippetDefinitionClasses(services);

            // Register feature classes
            services.AddSingleton<LightsaberAudio>();
            services.AddSingleton<TrashCan>();
            services.AddSingleton<StackDecrementPrevention>();
            services.AddSingleton<InstantItemUse>();
            services.AddSingleton<StandardItemConfigurations>();
            
            // Register item definition classes
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
            
            // Automatically register all ILootTableDefinition implementations
            var assembly = Assembly.GetExecutingAssembly();
            var lootTableDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ILootTableDefinition).IsAssignableFrom(t));
            
            foreach (var type in lootTableDefinitionTypes)
            {
                services.AddSingleton(type);
            }

            // Automatically register all IItemListDefinition implementations
            var itemDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IItemListDefinition).IsAssignableFrom(t));
            
            foreach (var type in itemDefinitionTypes)
            {
                services.AddSingleton(type);
            }

            return services;
        }

        private static void RegisterSnippetDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement ISnippetListDefinition across all assemblies
            var snippetDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ISnippetListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in snippetDefinitionTypes)
            {
                // Register each snippet definition class as transient
                services.AddSingleton(type);
                services.AddSingleton(typeof(ISnippetListDefinition), type);
            }
        }

    }
}
