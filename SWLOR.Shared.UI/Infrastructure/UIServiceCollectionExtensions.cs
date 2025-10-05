using Microsoft.Extensions.DependencyInjection;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.UI.EventHandlers;
using SWLOR.Shared.Core.Infrastructure;

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
            services.AddSingleton<IGuiService, GuiService>();
            services.AddSingleton<UIEventHandlers>();

            var viewModelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && 
                              !type.IsAbstract && 
                              IsViewModelType(type))
                .ToList();

            foreach (var viewModelType in viewModelTypes)
            {
                services.AddSingleton(viewModelType);
                
                if (typeof(IServiceInitializer).IsAssignableFrom(viewModelType))
                {
                    services.AddSingleton<IServiceInitializer>(provider => (IServiceInitializer)provider.GetRequiredService(viewModelType));
                }
            }

            services.RegisterInterfaceImplementations<IGuiWindowDefinition>();
            
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
