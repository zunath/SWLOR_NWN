using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Crafting.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Crafting-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Crafting services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCraftingServices(this IServiceCollection services)
        {
            // TODO: Add Crafting services here as they are implemented
            
            return services;
        }
    }
}
