using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.EventHandlers;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.Shared.Core.Infrastructure;
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
            // Register factory and service
            services.AddSingleton<IStatusEffectFactory, StatusEffectFactory>();
            services.AddSingleton<IStatusEffectService, StatusEffectService>();
            services.AddSingleton<StatusEffectEventHandler>();
            
            // Automatically register all status effect implementations
            services.RegisterInterfaceImplementations<IStatusEffect>();
            
            return services;
        }
    }
}
