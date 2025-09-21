using Microsoft.Extensions.DependencyInjection;

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
            // TODO: Add Market services here as they are implemented
            
            return services;
        }
    }
}
