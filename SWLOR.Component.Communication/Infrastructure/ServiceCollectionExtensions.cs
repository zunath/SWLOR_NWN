using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.EventHandlers;
using SWLOR.Component.Communication.Feature;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;

namespace SWLOR.Component.Communication.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Communication-related services in the dependency injection container.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Communication services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
        {
            // Register services as singletons
            services.AddSingleton<IChatCommandService, ChatCommand>();
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<ILanguageService, Language>();
            services.AddSingleton<IHoloComService, HoloComService>();
            services.AddSingleton<IRoleplayXPService, RoleplayXPService>();
            services.AddSingleton<ISnippetService, Snippet>();
            services.AddSingleton<IMessagingService, Messaging>();
            services.AddSingleton<IDialogService, Service.Dialog>();
            
            // Register dialog classes
            services.AddTransient<Dialog.DiceDialog>();

            // Register event handlers as singletons
            services.AddSingleton<CommunicationEventHandlers>();

            return services;
        }
    }
}
