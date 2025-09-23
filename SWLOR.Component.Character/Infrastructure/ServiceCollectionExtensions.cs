using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Character.Contracts;
using SWLOR.Component.Character.Service;

namespace SWLOR.Component.Character.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Player-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Player services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPlayerServices(this IServiceCollection services)
        {
            services.AddSingleton<IClientVersionCheck, ClientVersionCheck>();
            
            return services;
        }
    }
}
