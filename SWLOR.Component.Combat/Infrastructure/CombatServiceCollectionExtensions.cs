using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.EventHandlers;
using SWLOR.Component.Combat.Feature;
using SWLOR.Component.Combat.Service;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Component.Combat.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Combat-related services in the dependency injection container.
    /// </summary>
    public static class CombatServiceCollectionExtensions
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
            services.AddSingleton<IStatService, StatService>();
            services.AddSingleton<IEnmityService, EnmityService>();
            services.AddSingleton<IDeathService, DeathService>();

            // Register feature classes
            services.AddSingleton<EquipmentStats>();
            services.AddSingleton<EquipmentRestrictions>();
            services.AddSingleton<WeaponFeatConfiguration>();
            services.AddSingleton<NaturalRegeneration>();
            services.AddSingleton<PersistentHitPoints>();
            services.AddSingleton<CreatureDeathAnimation>();
            services.AddSingleton<FeatConfiguration>();
            services.AddSingleton<Feature.TrapDefinition.PitfallTrap>();
            
            // Register script handler classes
            services.AddSingleton<Feature.TrapDefinition.SpawnLarvaeOnSlugDeath>();
            
            return services;
        }
    }
}
