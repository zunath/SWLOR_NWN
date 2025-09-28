using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Space.Contracts;
using SWLOR.Component.Space.EventHandlers;
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
            services.AddSingleton<ISpaceService, Service.SpaceService>();
            services.AddSingleton<IShipBuilder, Service.ShipBuilder>();
            services.AddSingleton<IShipModuleBuilder, Service.ShipModuleBuilder>();
            services.AddSingleton<ISpaceObjectBuilder, Service.SpaceObjectBuilder>();
            
            // Register Space EventHandlers
            services.AddSingleton<SpaceEventHandler>();
            
            // Dialog classes are automatically registered by the Inventory component
            
            // Automatically register all ship and space object definition implementations
            var assembly = Assembly.GetExecutingAssembly();
            
            // Register IShipListDefinition implementations
            var shipDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IShipListDefinition).IsAssignableFrom(t));
            
            foreach (var type in shipDefinitionTypes)
            {
                services.AddSingleton(type);
            }
            
            // Register IShipModuleListDefinition implementations
            var shipModuleDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(t));
            
            foreach (var type in shipModuleDefinitionTypes)
            {
                services.AddSingleton(type);
            }
            
            // Register ISpaceObjectListDefinition implementations
            var spaceObjectDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISpaceObjectListDefinition).IsAssignableFrom(t));
            
            foreach (var type in spaceObjectDefinitionTypes)
            {
                services.AddSingleton(type);
            }
            
            // Register ISpawnListDefinition implementations
            var spawnDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ISpawnListDefinition).IsAssignableFrom(t));
            
            foreach (var type in spawnDefinitionTypes)
            {
                services.AddSingleton(type);
            }
            
            return services;
        }
    }
}
