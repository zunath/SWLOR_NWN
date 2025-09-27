using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.EventHandlers;
using SWLOR.Component.Combat.Feature;
using SWLOR.Component.Combat.Service;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Component.Combat.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Combat-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Combat services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCombatServices(this IServiceCollection services)
        {
            services.AddSingleton<IAttackOfOpportunityService, AttackOfOpportunityService>();
            services.AddSingleton<ICombatService, CombatService>();
            services.AddSingleton<ICombatPointService, CombatPointService>();
            services.AddSingleton<IStatService, Stat>();
            services.AddSingleton<IEnmityService, Service.Enmity>();
            services.AddSingleton<Death>();

            // Register feature classes
            services.AddTransient<EquipmentStats>();
            services.AddTransient<EquipmentRestrictions>();
            services.AddTransient<WeaponFeatConfiguration>();
            services.AddTransient<NaturalRegeneration>();
            services.AddTransient<PersistentHitPoints>();
            services.AddTransient<CreatureDeathAnimation>();
            
            // Register script handler classes
            services.AddTransient<Feature.TrapDefinition.SpawnLarvaeOnSlugDeath>();

            // Register event handlers as singletons
            services.AddSingleton<CombatEventHandler>();
            
            return services;
        }
    }
}
