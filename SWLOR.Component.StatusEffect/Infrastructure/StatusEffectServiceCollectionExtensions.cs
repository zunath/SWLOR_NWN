using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Definitions.Buff;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.Shared.Domain.StatusEffect.Contracts;

namespace SWLOR.Component.StatusEffect.Infrastructure
{
    /// <summary>
    /// Extension methods for registering StatusEffect-related services in the dependency injection container.
    /// </summary>
    public static class StatusEffectServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all StatusEffect services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddStatusEffectServices(this IServiceCollection services)
        {
            // Register status effect implementations
            services.AddSingleton<HasteStatusEffect>();
            
            // Register factory and service
            services.AddSingleton<IStatusEffectFactory, StatusEffectFactory>();
            services.AddSingleton<IStatusEffectService, StatusEffectService>();
            services.AddSingleton<EventHandlers.StatusEffectEventHandler>();
            
            return services;
        }
    }
}
