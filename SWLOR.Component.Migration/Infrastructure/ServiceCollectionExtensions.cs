using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.EventHandlers;
using SWLOR.Component.Migration.Service;

namespace SWLOR.Component.Migration.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Migration-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Migration services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMigrationServices(this IServiceCollection services)
        {
            // Register Migration service
            services.AddSingleton<IMigrationService, MigrationService>();
            
            // Register MigrationEventHandler as singleton
            services.AddSingleton<MigrationEventHandler>();
            
            return services;
        }
    }
}
