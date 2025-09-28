using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Associate.EventHandlers;
using SWLOR.Component.Associate.Service;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Feature.ItemDefinition;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;

namespace SWLOR.Component.Associate.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Associate-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Associate services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAssociateServices(this IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<IBeastMasteryService, BeastMasteryService>();
            services.AddSingleton<IDroidService, DroidService>();

            // Register droid personality classes
            services.AddSingleton<Model.DroidGeekyPersonality>();
            services.AddSingleton<Model.DroidPrissyPersonality>();
            services.AddSingleton<Model.DroidSarcasticPersonality>();
            services.AddSingleton<Model.DroidSlangPersonality>();
            services.AddSingleton<Model.DroidBlandPersonality>();
            services.AddSingleton<Model.DroidWorshipfulPersonality>();

            // Register event handlers as singletons
            services.AddSingleton<AssociateEventHandlers>();

            // Register item definitions
            services.AddSingleton<IItemListDefinition, BeastEggItemDefinition>();
            services.AddSingleton<BeastEggItemDefinition>();
            services.AddSingleton<DNAExtractorItemDefinition>();

            // Dynamically register all loot table definition classes
            RegisterLootTableDefinitionClasses(services);

            // Dynamically register all beast definition classes
            RegisterBeastDefinitionClasses(services);

            // Dynamically register all item definition classes
            RegisterItemDefinitionClasses(services);

            return services;
        }

        private static void RegisterLootTableDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement ILootTableDefinition
            var lootTableDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(ILootTableDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in lootTableDefinitionTypes)
            {
                // Register each loot table definition as transient
                services.AddSingleton(type);
            }
        }

        private static void RegisterBeastDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement IBeastListDefinition
            var beastDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IBeastListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in beastDefinitionTypes)
            {
                // Register each beast definition as transient
                services.AddSingleton(type);
            }
        }

        private static void RegisterItemDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement IItemListDefinition
            var itemDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IItemListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in itemDefinitionTypes)
            {
                // Register each item definition as transient
                services.AddSingleton(type);
            }
        }
    }
}
