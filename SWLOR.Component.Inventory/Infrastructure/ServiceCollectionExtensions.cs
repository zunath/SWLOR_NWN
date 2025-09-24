using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Inventory.Contracts;
using SWLOR.Component.Inventory.EventHandlers;
using SWLOR.Component.Inventory.Service;

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
            services.AddSingleton<InventoryEventHandlers>();
            services.AddSingleton<InventoryServiceEventHandlers>();

            return services;
        }
    }
}
