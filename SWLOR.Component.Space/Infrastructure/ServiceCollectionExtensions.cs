using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.EventHandlers;
using SWLOR.Component.Space.Service;
using SWLOR.Shared.Domain.Space.Contracts;
using SWLOR.Shared.Domain.World.Contracts;

namespace SWLOR.Component.Space.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Space-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Space services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddSpaceServices(this IServiceCollection services)
        {
            // Register Space services
            services.AddSingleton<ISpaceService, SpaceService>();
            services.AddTransient<IShipBuilder, ShipBuilder>();
            services.AddTransient<IShipModuleBuilder, ShipModuleBuilder>();
            services.AddTransient<ISpaceObjectBuilder, SpaceObjectBuilder>();
            
            // Register Space EventHandlers
            services.AddSingleton<SpaceEventHandler>();
            
            RegisterShipListDefinitions(services);
            RegisterShipModuleListDefinitions(services);
            RegisterSpaceObjectListDefinitions(services);
            
            return services;
        }

        private static void RegisterShipListDefinitions(IServiceCollection services)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var shipDefinitionTypes = allAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IShipListDefinition).IsAssignableFrom(t));

            foreach (var type in shipDefinitionTypes)
            {
                services.AddSingleton(typeof(IShipListDefinition), type);
            }
        }

        private static void RegisterShipModuleListDefinitions(IServiceCollection services)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var shipModuleDefinitionTypes = allAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(t));

            foreach (var type in shipModuleDefinitionTypes)
            {
                services.AddSingleton(typeof(IShipModuleListDefinition), type);
            }
        }

        private static void RegisterSpaceObjectListDefinitions(IServiceCollection services)
        {
            var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var spaceObjectDefinitionTypes = allAssemblies
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISpaceObjectListDefinition).IsAssignableFrom(t));

            foreach (var type in spaceObjectDefinitionTypes)
            {
                services.AddSingleton(typeof(ISpaceObjectListDefinition), type);
            }
        }
    }
}
