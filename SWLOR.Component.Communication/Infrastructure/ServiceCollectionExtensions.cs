using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Communication.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Communication-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Communication services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
        {
            // TODO: Add Communication services here as they are implemented
            
            return services;
        }
    }
}
