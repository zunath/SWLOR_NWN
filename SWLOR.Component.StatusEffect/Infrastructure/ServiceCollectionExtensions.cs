using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.StatusEffect.Contracts;
using SWLOR.Component.StatusEffect.Feature;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Contracts;

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
            services.AddSingleton<IStatusEffectService, Service.StatusEffectService>();
            
            // Register event handlers as singletons
            services.AddSingleton<EventHandlers.StatusEffectEventHandler>();

            // Register feature classes
            services.AddTransient<BuffTimer>();
            
            // Automatically register all IStatusEffectListDefinition implementations
            var assembly = Assembly.GetExecutingAssembly();
            var statusEffectDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IStatusEffectListDefinition).IsAssignableFrom(t));
            
            foreach (var type in statusEffectDefinitionTypes)
            {
                services.AddTransient(type);
            }
            
            return services;
        }
    }
}
