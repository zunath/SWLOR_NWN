using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Associate.EventHandlers;
using SWLOR.Component.Associate.Service;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Shared.Domain.Beasts.Contracts;
using SWLOR.Shared.Domain.Droids.Contracts;

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
            // Register services as singletons
            services.AddSingleton<IBeastMasteryService, BeastMasteryService>();
            services.AddSingleton<IDroidService, DroidService>();

            // Register event handlers as singletons
            services.AddSingleton<AssociateEventHandlers>();

            return services;
        }
    }
}
