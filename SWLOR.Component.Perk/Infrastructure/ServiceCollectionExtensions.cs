using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Perk.Contracts;
using SWLOR.Component.Perk.Service;

namespace SWLOR.Component.Perk.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Perk-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Perk services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPerkServices(this IServiceCollection services)
        {
            // Register PerkBuilder as transient since it's a builder pattern
            services.AddTransient<IPerkBuilder, PerkBuilder>();
            
            // Register PerkRequirementFactory as transient
            services.AddTransient<IPerkRequirementFactory, PerkRequirementFactory>();
            
            return services;
        }
    }
}
