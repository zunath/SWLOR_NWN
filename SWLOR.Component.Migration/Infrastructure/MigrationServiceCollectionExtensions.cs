using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Migration.Contracts;
using SWLOR.Component.Migration.EventHandlers;
using SWLOR.Component.Migration.Service;
using SWLOR.Shared.Domain.Migration.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Migration.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Migration-related services in the dependency injection container.
    /// </summary>
    public static class MigrationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Migration services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMigrationServices(this IServiceCollection services)
        {
            services.AddSingleton<IMigrationService, MigrationService>();
            services.AddSingleton<MigrationEventHandler>();
            
            services.RegisterInterfaceImplementations<IServerMigration>();
            services.RegisterInterfaceImplementations<IPlayerMigration>();
            
            return services;
        }
    }
}
