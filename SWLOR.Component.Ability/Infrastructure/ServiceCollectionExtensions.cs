using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.EventHandlers;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Devices;
using SWLOR.Component.Ability.Feature.AbilityDefinition.Force;
using SWLOR.Component.Ability.Feature.AbilityDefinition.General;
using SWLOR.Component.Ability.Service;
using SWLOR.Shared.Domain.Contracts;

namespace SWLOR.Component.Ability.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Ability-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Ability services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAbilityServices(this IServiceCollection services)
        {
                // Register services as singletons
                services.AddSingleton<IAbilityService, Service.Ability>();
                services.AddSingleton<IRecastService, Recast>();

            // Register ability definition classes as singletons
            services.AddSingleton<BurstOfSpeedAbilityDefinition>();
            services.AddSingleton<GasBombAbilityDefinition>();
            services.AddSingleton<StealthGeneratorAbilityDefinition>();
            services.AddSingleton<IncendiaryBombAbilityDefinition>();
            services.AddSingleton<DashAbilityDefinition>();

            // Register event handlers as singletons
            services.AddSingleton<AbilityEventHandlers>();

            return services;
        }
    }
}
