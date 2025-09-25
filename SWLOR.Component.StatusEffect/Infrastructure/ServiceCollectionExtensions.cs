using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Service;
using SWLOR.Shared.Domain.Combat.Contracts;

namespace SWLOR.Component.StatusEffect.Infrastructure
{
    /// <summary>
    /// Extension methods for registering StatusEffect-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all StatusEffect services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddStatusEffectServices(this IServiceCollection services)
        {
            // Register StatusEffect service
            services.AddSingleton<IStatusEffectService, Service.StatusEffect>();
            
            return services;
        }
    }
}
