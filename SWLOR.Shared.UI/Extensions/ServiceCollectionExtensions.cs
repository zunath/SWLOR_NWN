using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Shared.UI.Extensions
{
    /// <summary>
    /// Extension methods for registering UI-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all ViewModels that inherit from GuiViewModelBase in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddViewModels(this IServiceCollection services)
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
            }

            return services;
        }

        /// <summary>
        /// Registers all GUI Window Definitions that implement IGuiWindowDefinition in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddGuiWindowDefinitions(this IServiceCollection services)
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
