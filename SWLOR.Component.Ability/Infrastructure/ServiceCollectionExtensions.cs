using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Ability.Infrastructure
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
        public static IServiceCollection AddAbilityServices(this IServiceCollection services)
        {
            
            return services;
        }
    }
}
