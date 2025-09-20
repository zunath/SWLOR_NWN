using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Data;

namespace SWLOR.Game.Server
{
    /// <summary>
    /// Handles registration of services in the dependency injection container.
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all game services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddGameServices(this IServiceCollection services)
        {
            // Register database services
            services.AddDatabaseServices();
            
            // Register other game services here as they are created
            // services.AddScoped<IPlayerService, PlayerService>();
            // services.AddScoped<ICombatService, CombatService>();
            // services.AddScoped<IAreaService, AreaService>();
            // etc.
            
            return services;
        }

        /// <summary>
        /// Registers database services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
        {
            // Register the database service as a singleton since it manages Redis connections
            services.AddSingleton<IDatabaseService, DatabaseService>();
            
            return services;
        }
    }
}
