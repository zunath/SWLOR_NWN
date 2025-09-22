using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Component.Skill.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Properties-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Properties services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSkillServices(this IServiceCollection services)
        {
            
            return services;
        }
    }
}
