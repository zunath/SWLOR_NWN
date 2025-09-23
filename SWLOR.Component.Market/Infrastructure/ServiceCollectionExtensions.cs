using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Market.Contracts;
using SWLOR.Component.Market.EventHandlers;
using SWLOR.Component.Market.Feature;
using SWLOR.Component.Market.Service;

namespace SWLOR.Component.Market.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Market-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Market services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMarketServices(this IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<IPlayerMarketService, PlayerMarket>();
            
            // Register features as singletons
            services.AddSingleton<StoreManagement>();
            
            // Register event handlers as singletons
            services.AddSingleton<MarketEventHandlers>();
            
            return services;
        }
    }
}
