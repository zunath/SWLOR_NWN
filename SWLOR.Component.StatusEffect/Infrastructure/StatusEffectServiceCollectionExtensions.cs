using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Feature;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Core.Infrastructure;

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
            // Register data service as singleton (shared state)
            services.AddSingleton<Service.StatusEffectDataService>();
            services.AddSingleton<IStatusEffectDataService>(provider => provider.GetRequiredService<Service.StatusEffectDataService>());

            // Register focused services
            services.AddSingleton<IStatusEffectApplicationService, Service.StatusEffectApplicationService>();
            services.AddSingleton<IStatusEffectManagementService, Service.StatusEffectManagementService>();
            services.AddSingleton<IStatusEffectQueryService, Service.StatusEffectQueryService>();
            services.AddSingleton<IStatusEffectIconService, Service.StatusEffectIconService>();

            // Register main StatusEffect service (facade)
            services.AddSingleton<IStatusEffectService, Service.StatusEffectService>();
            
            // Register event handlers as singletons
            services.AddSingleton<EventHandlers.StatusEffectEventHandler>();

            // Register feature classes
            services.AddSingleton<BuffTimer>();
            
            // Automatically register all IStatusEffectListDefinition implementations
            services.RegisterInterfaceImplementations<IStatusEffectListDefinition>();
            
            return services;
        }
    }
}
