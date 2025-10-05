using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Communication.Contracts;
using SWLOR.Component.Communication.EventHandlers;
using SWLOR.Component.Communication.Service;
using SWLOR.Shared.Domain.Communication.Contracts;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;
using SWLOR.Shared.Core.Infrastructure;

namespace SWLOR.Component.Communication.Infrastructure
{
    /// <summary>
    /// Extension methods for registering Communication-related services in the dependency injection container.
    /// </summary>
    public static class CommunicationServiceCollectionExtensions
    {
        /// <summary>
        /// Registers all Communication services in the service collection.
        /// </summary>
        /// <param name="services">The service collection to register services in</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddCommunicationServices(this IServiceCollection services)
        {
            services.AddSingleton<IChatCommandService, ChatCommandService>();
            services.AddSingleton<ICommunicationService, CommunicationService>();
            services.AddSingleton<ILanguageService, LanguageService>();
            services.AddSingleton<IHoloComService, HoloComService>();
            services.AddSingleton<IRoleplayXPService, RoleplayXPService>();
            services.AddSingleton<ISnippetService, SnippetService>();
            services.AddSingleton<IMessagingService, MessagingService>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddTransient<IDialogBuilder, DialogBuilder>();
            services.AddSingleton<IChatCommandBuilder, ChatCommandBuilder>();
            services.AddSingleton<ISnippetBuilder, SnippetBuilder>();
            services.AddSingleton<CommunicationEventHandlers>();
            services.AddSingleton<CommunicationServiceEventHandlers>();

            services.RegisterInterfaceImplementations<IChatCommandListDefinition>();

            var dialogTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(DialogBase).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in dialogTypes)
            {
                services.AddTransient(type);
            }

            return services;
        }
    }
}
