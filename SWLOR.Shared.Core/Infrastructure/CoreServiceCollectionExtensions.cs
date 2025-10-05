using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Core.Service;

namespace SWLOR.Shared.Core.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Core services in the dependency injection container.
    /// </summary>
    public static class CoreServiceCollectionExtensions
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
            services.AddSingleton<IServiceInitializer, ServiceInitializer>();

            return services;
        }

        /// <summary>
        /// Automatically registers all concrete types that implement the specified interface as singletons.
        /// This is a generic helper for the common pattern of discovering and registering interface implementations.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to find implementations for</typeparam>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection RegisterInterfaceImplementations<TInterface>(this IServiceCollection services)
            where TInterface : class
        {
            var implementationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(TInterface).IsAssignableFrom(type) && 
                              !type.IsInterface && 
                              !type.IsAbstract);

            foreach (var type in implementationTypes)
            {
                services.AddSingleton(type);
            }

            return services;
        }

        /// <summary>
        /// Automatically registers all concrete types that implement the specified interface as singletons,
        /// and also registers them with their interface type for dependency injection.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to find implementations for</typeparam>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection RegisterInterfaceImplementationsWithInterface<TInterface>(this IServiceCollection services)
            where TInterface : class
        {
            var implementationTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(TInterface).IsAssignableFrom(type) && 
                              !type.IsInterface && 
                              !type.IsAbstract);

            foreach (var type in implementationTypes)
            {
                services.AddSingleton(type);
                services.AddSingleton(typeof(TInterface), type);
            }

            return services;
        }
    }
}
