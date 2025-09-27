using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Ability.Contracts;
using SWLOR.Component.Ability.EventHandlers;
using SWLOR.Component.Ability.Service;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Common.Contracts;

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
                services.AddSingleton<IAbilityService, AbilityService>();
                services.AddSingleton<IRecastService, RecastService>();
                services.AddSingleton<IAbilityBuilder, AbilityBuilder>();

            // Dynamically register all ability definition classes
            RegisterAbilityDefinitionClasses(services);

            // Register event handlers as singletons
            services.AddSingleton<AbilityEventHandlers>();

            return services;
        }

        private static void RegisterAbilityDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement IAbilityListDefinition
            var abilityDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IAbilityListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in abilityDefinitionTypes)
            {
                // Register each ability definition as singleton
                services.AddSingleton(type);
            }
        }
    }
}
