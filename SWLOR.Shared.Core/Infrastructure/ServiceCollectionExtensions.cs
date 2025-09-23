using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Shared.Core.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Core services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Core services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddSingleton<IRandomService, RandomService>();
            services.AddSingleton<ITimeService, TimeService>();
            services.AddSingleton<IDiscordNotificationService, DiscordNotificationService>();
            
            return services;
        }
    }
}
