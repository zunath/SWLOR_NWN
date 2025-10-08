using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.EventHandlers;
using SWLOR.Component.Ability.Service;
using SWLOR.Shared.Abstractions.Extensions;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Ability.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Ability-related services in the dependency injection container.
    /// </summary>
    public static class AbilityServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Ability services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAbilityServices(this IServiceCollection services)
        {
            services.AddSingleton<IAbilityService, AbilityService>();
            services.AddSingleton<IRecastService, RecastService>();
            services.AddSingleton<IAbilityBuilder, AbilityBuilder>();
            services.AddSingleton<AbilityEventHandlers>();

            services.RegisterInterfaceImplementations<IAbilityListDefinition>();

            return services;
        }
    }
}
