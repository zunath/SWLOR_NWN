using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Skill.EventHandlers;
using SWLOR.Component.Skill.Service;
using SWLOR.Shared.Domain.Skill.Contracts;

namespace SWLOR.Component.Skill.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Skill-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Skill services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSkillServices(this IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<ISkillService, SkillService>();
            
            // Register event handlers as singletons
            services.AddSingleton<SkillEventHandlers>();

            // Snippet definitions are automatically registered by the Inventory component
            
            return services;
        }
    }
}
