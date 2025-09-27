using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.EventHandlers;
using SWLOR.Component.Migration.Service;
using SWLOR.Shared.Domain.Common.Contracts;

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
            
            // Dynamically register all migration classes
            RegisterMigrationClasses(services);
            
            return services;
        }

        private static void RegisterMigrationClasses(IServiceCollection services)
        {
            // Find all types that implement IServerMigration
            var serverMigrationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IServerMigration).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var migrationType in serverMigrationTypes)
            {
                // Register each server migration as transient
                services.AddTransient(migrationType);
            }

            // Find all types that implement IPlayerMigration
            var playerMigrationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IPlayerMigration).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var migrationType in playerMigrationTypes)
            {
                // Register each player migration as transient
                services.AddTransient(migrationType);
            }
        }
    }
}
