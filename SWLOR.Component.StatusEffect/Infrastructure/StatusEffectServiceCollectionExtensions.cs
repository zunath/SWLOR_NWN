using Microsoft.Extensions.DependencyInjection;

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
            
            services.AddSingleton<EventHandlers.StatusEffectEventHandler>();
            
            
            return services;
        }
    }
}
