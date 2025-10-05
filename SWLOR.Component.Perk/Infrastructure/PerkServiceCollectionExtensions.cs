using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.EventHandlers;
using SWLOR.Component.Perk.Service;
using SWLOR.Shared.Domain.Perk.Contracts;

namespace SWLOR.Component.Perk.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Perk-related services in the dependency injection container.
    /// </summary>
    public static class PerkServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Perk services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPerkServices(this IServiceCollection services)
        {
            // Register PerkBuilder as transient since it's a builder pattern
            services.AddSingleton<IPerkBuilder, PerkBuilder>();
            
            // Register PerkRequirementFactory as transient
            services.AddSingleton<IPerkRequirementFactory, PerkRequirementFactory>();
            
            // Register UsePerkFeat as singleton
            services.AddSingleton<IUsePerkFeat, UsePerkFeat>();
            
            // Register PerkEffectService as singleton
            services.AddSingleton<IPerkEffectService, PerkEffectService>();
            
            // Register Perk service as singleton
            services.AddSingleton<IPerkService, PerkService>();
            
            // Register PerkEventHandler as singleton
            services.AddSingleton<PerkEventHandler>();
            
            // Automatically register all IPerkListDefinition implementations
            var assembly = Assembly.GetExecutingAssembly();
            var perkDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(t));
            
            foreach (var type in perkDefinitionTypes)
            {
                services.AddSingleton(type);
            }
            
            return services;
        }
    }
}
