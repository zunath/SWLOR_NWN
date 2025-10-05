using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.EventHandlers;
using SWLOR.Component.Market.Feature;
using SWLOR.Component.Market.Repository;
using SWLOR.Component.Market.Service;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.Component.Market.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Market-related services in the dependency injection container.
    /// </summary>
    public static class MarketServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Market services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMarketServices(this IServiceCollection services)
        {
            // Register repositories
            services.AddSingleton<IMarketItemRepository, MarketItemRepository>();
            
            // Register services as singletons
            services.AddSingleton<IPlayerMarketService, PlayerMarketService>();
            
            // Register features as singletons
            services.AddSingleton<StoreManagement>();
            
            // Register event handlers as singletons
            services.AddSingleton<MarketEventHandlers>();

            // Snippet definitions are automatically registered by the Inventory component

            // Dialog classes are automatically registered by the Inventory component
            
            return services;
        }
    }
}
