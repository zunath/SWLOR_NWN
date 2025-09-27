using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Properties.Contracts;
using SWLOR.Component.Properties.EventHandlers;
using SWLOR.Component.Properties.Service;
using SWLOR.Shared.Domain.Properties.Contracts;

namespace SWLOR.Component.Properties.Infrastructure
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
        public static IServiceCollection AddPropertiesServices(this IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<IPropertyService, PropertyService>();
            services.AddSingleton<PropertyService>(); // Register concrete type for direct injection
            services.AddSingleton<StructureChangedAction>();

            // Register event handlers as singletons
            services.AddSingleton<PropertiesEventHandlers>();
            
            // Dialog classes are automatically registered by the Inventory component
            
            // Automatically register all IPropertyLayoutListDefinition implementations
            var assembly = Assembly.GetExecutingAssembly();
            var propertyLayoutDefinitionTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IPropertyLayoutListDefinition).IsAssignableFrom(t));
            
            foreach (var type in propertyLayoutDefinitionTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}
