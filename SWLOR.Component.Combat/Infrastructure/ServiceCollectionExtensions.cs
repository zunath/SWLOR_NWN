using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Combat.Contracts;
using SWLOR.Component.Combat.Service;

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
            
            return services;
        }
    }
}
