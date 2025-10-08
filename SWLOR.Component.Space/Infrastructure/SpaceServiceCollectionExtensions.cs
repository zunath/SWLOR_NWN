using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.EventHandlers;
using SWLOR.Component.Space.Repository;
using SWLOR.Component.Space.Service;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Space.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Space-related services in the dependency injection container.
    /// </summary>
    public static class SpaceServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Space services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSpaceServices(this IServiceCollection services)
        {
            services.AddSingleton<IPlayerShipRepository, PlayerShipRepository>();
            services.AddSingleton<ISpaceService, SpaceService>();
            services.AddSingleton<SpaceEventHandler>();
            services.AddTransient<IShipBuilder, ShipBuilder>();
            services.AddTransient<IShipModuleBuilder, ShipModuleBuilder>();
            services.AddTransient<ISpaceObjectBuilder, SpaceObjectBuilder>();

            services.RegisterInterfaceImplementationsWithInterface<IShipListDefinition>();
            services.RegisterInterfaceImplementationsWithInterface<IShipModuleListDefinition>();
            services.RegisterInterfaceImplementationsWithInterface<ISpaceObjectListDefinition>();

            return services;
        }
    }
}
