using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Core.Contracts;

namespace SWLOR.Shared.UI.Infrastructure
{
    /// <summary>
    /// Extension methods for registering UI-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all UI-related services in the service collection.
        /// This includes the GuiService, ViewModels, and GUI Window Definitions.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddUIServices(this IServiceCollection services)
        {
            // Register the main GUI service
            services.AddSingleton<IGuiService, GuiService>();
            
            // Register all ViewModels that inherit from GuiViewModelBase
            AddViewModels(services);
            
            // Register all GUI Window Definitions that implement IGuiWindowDefinition
            AddGuiWindowDefinitions(services);
            
            return services;
        }

        /// <summary>
        /// Registers all ViewModels that inherit from GuiViewModelBase in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        private static IServiceCollection AddViewModels(IServiceCollection services)
        {
            // Automatically discover and register all ViewModels that inherit from GuiViewModelBase across all assemblies
            var viewModelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && 
                              !type.IsAbstract && 
                              IsViewModelType(type))
                .ToList();

            foreach (var viewModelType in viewModelTypes)
            {
                services.AddTransient(viewModelType);
                
                // If the ViewModel also implements IServiceInitializer, register it as such
                if (typeof(IServiceInitializer).IsAssignableFrom(viewModelType))
                {
                    services.AddTransient<IServiceInitializer>(provider => (IServiceInitializer)provider.GetRequiredService(viewModelType));
                }
            }

            return services;
        }

        /// <summary>
        /// Registers all GUI Window Definitions that implement IGuiWindowDefinition in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        private static IServiceCollection AddGuiWindowDefinitions(IServiceCollection services)
        {
            // Automatically discover and register all GUI Window Definitions that implement IGuiWindowDefinition across all assemblies
            var windowDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && 
                              !type.IsAbstract && 
                              typeof(IGuiWindowDefinition).IsAssignableFrom(type))
                .ToList();

            foreach (var windowDefinitionType in windowDefinitionTypes)
            {
                services.AddTransient(windowDefinitionType);
            }

            return services;
        }

        /// <summary>
        /// Checks if a type is a ViewModel that inherits from GuiViewModelBase.
        /// </summary>
        /// <param name="type">The type to check</param>
        /// <returns>True if the type is a ViewModel, false otherwise</returns>
        private static bool IsViewModelType(Type type)
        {
            // Check if it inherits from GuiViewModelBase<,>
            var baseType = type.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && 
                    baseType.GetGenericTypeDefinition() == typeof(GuiViewModelBase<,>))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }

            return false;
        }
    }
}
