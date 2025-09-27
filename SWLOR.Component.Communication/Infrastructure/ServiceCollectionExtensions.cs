using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.EventHandlers;
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
            services.AddSingleton<IChatCommandService, ChatCommandService>();
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<ILanguageService, LanguageService>();
            services.AddSingleton<IHoloComService, HoloComService>();
            services.AddSingleton<IRoleplayXPService, RoleplayXPService>();
            services.AddSingleton<ISnippetService, SnippetService>();
            services.AddSingleton<IMessagingService, MessagingService>();
            services.AddSingleton<IDialogService, Service.DialogService>();
            services.AddSingleton<IDialogBuilder, DialogBuilder>();
            services.AddSingleton<IChatCommandBuilder, ChatCommandBuilder>();
            services.AddSingleton<ISnippetBuilder, SnippetBuilder>();
            
                // Dialog classes are automatically registered by the Inventory component

            // Dynamically register all chat command definition classes
            RegisterChatCommandDefinitionClasses(services);

            // Register event handlers as singletons
            services.AddSingleton<CommunicationEventHandlers>();

            return services;
        }

        private static void RegisterChatCommandDefinitionClasses(IServiceCollection services)
        {
            // Find all types that implement IChatCommandListDefinition, excluding Space component chat commands
            var chatCommandDefinitionTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IChatCommandListDefinition).IsAssignableFrom(w) && 
                           !w.IsInterface && 
                           !w.IsAbstract &&
                           !w.Namespace?.Contains("SWLOR.Component.Space") == true);

            foreach (var type in chatCommandDefinitionTypes)
            {
                // Register each chat command definition as transient
                services.AddTransient(type);
            }
        }
    }
}
