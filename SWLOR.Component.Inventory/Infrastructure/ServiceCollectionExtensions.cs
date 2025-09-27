using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.EventHandlers;
using SWLOR.Component.Inventory.Feature;
using SWLOR.Component.Inventory.Service;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Inventory.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Inventory-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Inventory services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddInventoryServices(this IServiceCollection services)
        {
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
            services.AddTransient<LightsaberAudio>();
            
            // Automatically register all dialog classes
            RegisterDialogClasses(services);

            // Register item definition classes
            services.AddTransient<Feature.ItemDefinition.FishingRodItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.DestroyItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.ConsumableItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.TomeItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.DroidControlItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.KeyItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.HarvesterItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.RecipeItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.SpeederItemDefinition>();
            services.AddTransient<Feature.ItemDefinition.SaberUpgradeItemDefinition>();
            
            // Automatically register all ILootTableDefinition implementations
            var assembly = Assembly.GetExecutingAssembly();
            var lootTableDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ILootTableDefinition).IsAssignableFrom(t));
            
            foreach (var type in lootTableDefinitionTypes)
            {
                services.AddTransient(type);
            }

            // Automatically register all IItemListDefinition implementations
            var itemDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IItemListDefinition).IsAssignableFrom(t));
            
            foreach (var type in itemDefinitionTypes)
            {
                services.AddTransient(type);
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
                services.AddTransient(type);
                services.AddTransient(typeof(ISnippetListDefinition), type);
            }
        }

        private static void RegisterDialogClasses(IServiceCollection services)
        {
            // Find all types that inherit from DialogBase across all assemblies
            var dialogTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(DialogBase).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in dialogTypes)
            {
                // Register each dialog class as transient
                services.AddTransient(type);
            }
        }
    }
}
