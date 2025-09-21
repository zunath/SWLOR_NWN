using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Space.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Space-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Space services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSpaceServices(this IServiceCollection services)
        {
            // TODO: Add Space services here as they are implemented
            
            return services;
        }
    }
}
