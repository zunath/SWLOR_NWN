using Microsoft.Extensions.DependencyInjection;

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
            // TODO: Add Associate services here as they are implemented
            
            return services;
        }
    }
}
