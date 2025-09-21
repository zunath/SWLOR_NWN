using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Language.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Language-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Language services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddLanguageServices(this IServiceCollection services)
        {
            // TODO: Add Language services here as they are implemented
            
            return services;
        }
    }
}
