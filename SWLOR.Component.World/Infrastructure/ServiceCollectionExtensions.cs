using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.World.Contracts;
using SWLOR.Component.World.Service;

namespace SWLOR.Component.World.Infrastructure
{
    /// <summary>
    /// Extension methods for registering World-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all World services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddWorldServices(this IServiceCollection services)
        {
            services.AddSingleton<ITileMagicService, TileMagicService>();
            services.AddSingleton<ITaxiService, Taxi>();

            return services;
        }
    }
}
