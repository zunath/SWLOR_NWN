using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Admin.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Admin-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Admin services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAdminServices(this IServiceCollection services)
        {
            // TODO: Add Admin services here as they are implemented
            
            return services;
        }
    }
}
