using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.AI.Contracts;
using SWLOR.Component.AI.EventHandlers;
using SWLOR.Component.AI.Model;
using SWLOR.Component.AI.Service;

namespace SWLOR.Component.AI.Infrastructure
{
    /// <summary>
    /// Extension methods for registering AI-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all AI services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAIServices(this IServiceCollection services)
        {
            // Register AI service (business logic)
            services.AddSingleton<IAIService, AIService>();
            
            // Register AI event handlers (infrastructure)
            services.AddSingleton<AIEventHandlers>();
            
            // Register AI definitions
            services.AddSingleton<IAIDefinition, GenericAIDefinition>();
            services.AddSingleton<IAIDefinition, DroidAIDefinition>();
            services.AddSingleton<IAIDefinition, BeastAIDefinition>();
            
            return services;
        }
    }
}
